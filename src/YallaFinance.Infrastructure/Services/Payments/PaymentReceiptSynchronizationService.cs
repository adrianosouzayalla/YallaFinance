using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Services.Payments.CreditCard;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;
using YallaFinance.Application.Services.Payments.Boleto;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Application.Services.Payments.Pix;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.CreditCard;
using YallaFinance.Infrastructure.Providers.Payments.Boleto;
using YallaFinance.Infrastructure.Providers.Payments.Pix;

namespace YallaFinance.Infrastructure.Services.Payments;

internal sealed class PaymentReceiptSynchronizationService(
    ICompanyConnectionRepository connections,
    IProviderResolver<IPixReceivedProvider> pixProviderResolver,
    IProviderResolver<ICreditCardPaymentProvider> cardProviderResolver,
    IProviderResolver<IBoletoPaymentProvider> boletoProviderResolver,
    IPixService pix,
    ICreditCardService cards,
    IBoletoService boletos,
    IPaymentRepository payments,
    PaymentReceiptSynchronizationCursor reconciliationCursor,
    ILogger<PaymentReceiptSynchronizationService> logger) : IPaymentReceiptSynchronizationService
{
    private const int PixPageSize = 100;
    private const int CardBatchSize = 100;

    public async Task SynchronizeAsync(CancellationToken cancellationToken)
    {
        var enabledConnections = await connections.ListEnabledRuntimeAsync("PaymentProvider", cancellationToken);
        foreach (var connectionRecord in enabledConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var connection = new ProviderConnection(
                connectionRecord.Id,
                connectionRecord.TenantId,
                connectionRecord.Provider,
                connectionRecord.Name,
                connectionRecord.Enabled,
                connectionRecord.Group);
            if (pixProviderResolver.CanResolve(connection.Provider))
                await SynchronizeConnectionCapabilityAsync(
                    connection,
                    "Pix",
                    () => SynchronizePixReceiptsAsync(connection, cancellationToken),
                    cancellationToken);
            if (cardProviderResolver.CanResolve(connection.Provider))
                await SynchronizeConnectionCapabilityAsync(
                    connection,
                    "CreditCard",
                    () => SynchronizeCardReceiptsAsync(connection, cancellationToken),
                    cancellationToken);
            if (boletoProviderResolver.CanResolve(connection.Provider))
                await SynchronizeConnectionCapabilityAsync(
                    connection,
                    "Boleto",
                    () => SynchronizeBoletoReceiptsAsync(connection, cancellationToken),
                    cancellationToken);
        }
    }

    private async Task SynchronizeConnectionCapabilityAsync(
        ProviderConnection connection,
        string capability,
        Func<Task> synchronizeCapability,
        CancellationToken cancellationToken)
    {
        try
        {
            await synchronizeCapability();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Payment receipt synchronization failed for a connection capability. ConnectionId {ConnectionId}; Capability {Capability}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                connection.Id,
                capability,
                exception.GetType().Name,
                exception.GetBaseException().GetType().Name);
        }
    }

    private async Task SynchronizePixReceiptsAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var provider = pixProviderResolver.Resolve(connection.Provider);
        var window = await provider.ResolveDefaultWindowAsync(connection, cancellationToken);
        var segments = PixReceivedWindowPlanner.Split(window, provider.WindowRule);
        if (segments.Count == 0)
        {
            logger.LogWarning(
                "Pix receipt synchronization did not receive a valid provider query window. ConnectionId {ConnectionId}",
                connection.Id);
            return;
        }

        foreach (var segment in segments)
        {
            // Um segmento que nao conclui interrompe a sincronizacao: nada e reportado como concluido.
            if (!await SynchronizePixReceiptSegmentAsync(connection, segment, cancellationToken))
                return;
        }
    }

    private async Task<bool> SynchronizePixReceiptSegmentAsync(
        ProviderConnection connection,
        PixReceivedWindow segment,
        CancellationToken cancellationToken)
    {
        var page = 0;
        while (true)
        {
            var result = await pix.QueryAsync(
                new PixReceivedQuery(
                    connection.TenantId,
                    connection.Id,
                    connection.Provider,
                    segment.Start,
                    segment.End,
                    Page: page,
                    ItemsPerPage: PixPageSize),
                cancellationToken);
            if (result.Outcome != PixReceivedQueryOutcome.Completed)
            {
                logger.LogWarning(
                    "Pix receipt synchronization query did not complete. ConnectionId {ConnectionId}; Provider {Provider}; Page {Page}; Outcome {Outcome}; ErrorCode {ErrorCode}",
                    connection.Id,
                    connection.Provider,
                    page,
                    result.Outcome,
                    result.ErrorCode);
                return false;
            }

            if (result.ConnectionId != connection.Id
                || !string.Equals(result.Provider, connection.Provider, StringComparison.Ordinal)
                || result.Pagination is null)
            {
                logger.LogWarning(
                    "Pix receipt synchronization received an invalid query identity or pagination. ConnectionId {ConnectionId}; Page {Page}",
                    connection.Id,
                    page);
                return false;
            }

            if (result.Start != segment.Start || result.End != segment.End)
            {
                logger.LogWarning(
                    "Pix receipt synchronization received a different window from the requested one. ConnectionId {ConnectionId}; Page {Page}",
                    connection.Id,
                    page);
                return false;
            }

            var persistence = await payments.PersistReceivedReceiptsAsync(
                connection.TenantId,
                connection.Id,
                connection.Provider,
                result.Transactions,
                cancellationToken);
            if (persistence.IsFailure)
            {
                logger.LogError(
                    "Pix receipt synchronization persistence failed. ConnectionId {ConnectionId}; Page {Page}; ErrorCode {ErrorCode}",
                    connection.Id,
                    page,
                    persistence.Errors.FirstOrDefault()?.Code);
                return false;
            }

            var persistenceSummary = persistence.Value!;
            logger.LogInformation(
                "Pix receipt synchronization page completed. ConnectionId {ConnectionId}; Page {Page}; WindowStartUtc {WindowStartUtc}; WindowEndUtc {WindowEndUtc}; Returned {Returned}; WithTxId {WithTxId}; Matched {Matched}; AlreadyStored {AlreadyStored}; Unmatched {Unmatched}; Stored {Stored}; ProviderTotalItems {ProviderTotalItems}; ProviderTotalPages {ProviderTotalPages}",
                connection.Id,
                page,
                segment.Start,
                segment.End,
                result.Transactions.Count,
                persistenceSummary.WithTxId,
                persistenceSummary.Matched,
                persistenceSummary.AlreadyStored,
                persistenceSummary.Unmatched,
                persistenceSummary.Stored,
                result.Pagination.TotalItems,
                result.Pagination.TotalPages);

            var pagination = result.Pagination;
            if (pagination.Page != page || pagination.ItemsPerPage != PixPageSize
                || pagination.TotalPages < 0 || pagination.TotalItems < result.Transactions.Count)
            {
                logger.LogWarning(
                    "Pix receipt synchronization pagination was inconsistent. ConnectionId {ConnectionId}; Page {Page}",
                    connection.Id,
                    page);
                return false;
            }
            if (page + 1 >= pagination.TotalPages)
                return true;
            page++;
            if (page > 1000)
            {
                logger.LogError(
                    "Pix receipt synchronization stopped at the safety page limit. ConnectionId {ConnectionId}",
                    connection.Id);
                return false;
            }
        }
    }

    private async Task SynchronizeCardReceiptsAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var candidates = await payments.ListCardPaymentReconciliationCandidatesAsync(
            connection.TenantId,
            connection.Id,
            CardBatchSize,
            reconciliationCursor.ReadOffset(connection.TenantId, connection.Id, "CreditCard"),
            cancellationToken);
        if (candidates.Count == 0 || candidates.Count < CardBatchSize)
            reconciliationCursor.Reset(connection.TenantId, connection.Id, "CreditCard");
        else
            reconciliationCursor.AdvanceOffset(
                connection.TenantId,
                connection.Id,
                "CreditCard",
                candidates.Count);

        var confirmedCount = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(candidate.Attempt.Provider, connection.Provider, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "Credit card reconciliation skipped after provider identity changed. AttemptId {AttemptId}; ConnectionId {ConnectionId}",
                    candidate.Attempt.Id,
                    connection.Id);
                continue;
            }

            try
            {
                var query = new CreditCardChargeQuery(
                    new CreditCardConnectionContext(connection.TenantId, connection.Id, candidate.Attempt.Provider),
                    candidate.ProviderChargeId);
                var providerResult = await cards.GetChargeAsync(query, cancellationToken);
                if (providerResult.Outcome != CreditCardOperationOutcome.Completed || providerResult.Value is null)
                {
                    logger.LogWarning(
                        "Credit card reconciliation query did not complete. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Outcome {Outcome}; ErrorCode {ErrorCode}",
                        candidate.Attempt.Id,
                        connection.Id,
                        providerResult.Outcome,
                        providerResult.ErrorCode);
                    continue;
                }

                var charge = providerResult.Value;
                if (charge.State is not (CreditCardPaymentState.Confirmed or CreditCardPaymentState.Received))
                {
                    await payments.PersistCardResultAsync(candidate.Attempt, charge, cancellationToken);
                    continue;
                }

                await payments.PersistCardResultAsync(candidate.Attempt, charge, cancellationToken);
                confirmedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Credit card reconciliation failed for an attempt. AttemptId {AttemptId}; ConnectionId {ConnectionId}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                    candidate.Attempt.Id,
                    connection.Id,
                    exception.GetType().Name,
                    exception.GetBaseException().GetType().Name);
            }
        }

        logger.LogInformation(
            "Credit card reconciliation batch completed. ConnectionId {ConnectionId}; Provider {Provider}; Queried {Queried}; Confirmed {Confirmed}",
            connection.Id,
            connection.Provider,
            candidates.Count,
            confirmedCount);
    }

    private async Task SynchronizeBoletoReceiptsAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var candidates = await payments.ListBoletoPaymentReconciliationCandidatesAsync(
            connection.TenantId,
            connection.Id,
            CardBatchSize,
            reconciliationCursor.ReadOffset(connection.TenantId, connection.Id, "Boleto"),
            cancellationToken);
        if (candidates.Count == 0 || candidates.Count < CardBatchSize)
            reconciliationCursor.Reset(connection.TenantId, connection.Id, "Boleto");
        else
            reconciliationCursor.AdvanceOffset(connection.TenantId, connection.Id, "Boleto", candidates.Count);

        var confirmedCount = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(candidate.Attempt.Provider, connection.Provider, StringComparison.Ordinal))
                continue;

            try
            {
                var result = await boletos.GetAsync(new BoletoChargeQuery(
                    new BoletoConnectionContext(connection.TenantId, connection.Id, candidate.Attempt.Provider),
                    candidate.Attempt.Id,
                    candidate.Attempt.ProviderExternalId,
                    candidate.ProviderPaymentId), cancellationToken);
                if (result.Outcome != BoletoOperationOutcome.Completed || result.Value is null)
                {
                    logger.LogWarning(
                        "Boleto reconciliation query did not complete. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Outcome {Outcome}; ErrorCode {ErrorCode}",
                        candidate.Attempt.Id,
                        connection.Id,
                        result.Outcome,
                        result.ErrorCode);
                    continue;
                }

                await payments.PersistBoletoResultAsync(candidate.Attempt, result.Value, cancellationToken);
                if (result.Value.State is BoletoPaymentState.Confirmed or BoletoPaymentState.Received)
                    confirmedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Boleto reconciliation failed for an attempt. AttemptId {AttemptId}; ConnectionId {ConnectionId}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                    candidate.Attempt.Id,
                    connection.Id,
                    exception.GetType().Name,
                    exception.GetBaseException().GetType().Name);
            }
        }

        logger.LogInformation(
            "Boleto reconciliation batch completed. ConnectionId {ConnectionId}; Provider {Provider}; Queried {Queried}; Confirmed {Confirmed}",
            connection.Id,
            connection.Provider,
            candidates.Count,
            confirmedCount);
    }
}

internal sealed class PaymentReceiptSynchronizationCursor
{
    private readonly ConcurrentDictionary<(Guid TenantId, Guid ConnectionId, string Capability), int> offsetByConnection = new();

    public int ReadOffset(Guid tenantId, Guid connectionId, string capability) =>
        offsetByConnection.TryGetValue((tenantId, connectionId, capability), out var offset) ? offset : 0;

    public void AdvanceOffset(Guid tenantId, Guid connectionId, string capability, int itemCount) =>
        offsetByConnection.AddOrUpdate((tenantId, connectionId, capability), itemCount, (_, currentOffset) => currentOffset + itemCount);

    public void Reset(Guid tenantId, Guid connectionId, string capability) =>
        offsetByConnection.TryRemove((tenantId, connectionId, capability), out _);
}
