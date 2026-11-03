using System.Globalization;
using Microsoft.Extensions.Logging;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.Pix;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments.Pix;

internal sealed class AsaasPixReceivedProvider(
    AsaasConfigurationReader configuration,
    AsaasPixOperations operations,
    TimeProvider clock,
    ILogger<AsaasPixReceivedProvider> logger) : IPixReceivedProvider
{
    public string Provider => "Asaas";

    // A consulta de Pix recebidos do Asaas aceita intervalos arbitrarios.
    public PixReceivedWindowRule WindowRule => PixReceivedWindowRule.Unrestricted;

    public async Task<PixReceivedWindow> ResolveDefaultWindowAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var options = await configuration.ReadAsync(connection, cancellationToken);
        var days = PixReceivedDefaults.BaseLookbackDays;
        if (options.PixReceivedLookbackDays is { Length: > 0 } configuredDays
            && (!int.TryParse(configuredDays, NumberStyles.None,
                CultureInfo.InvariantCulture, out days) || days is < 1 or > 31))
            throw new IntegrationConfigurationException("PixReceivedLookbackDaysInvalid");
        var end = clock.GetUtcNow();
        return new(end.AddDays(-days), end);
    }

    public async Task<PixReceivedQueryResult> QueryAsync(
        PixReceivedQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        PixReceivedQueryResult Result(
            PixReceivedQueryOutcome outcome,
            string? error = null,
            IReadOnlyList<PixReceivedTransaction>? transactions = null,
            PixReceivedPagination? pagination = null,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null) =>
            new(outcome, connection.Id, Provider, transactions ?? [], pagination, error, start, end);

        if (!IsValidQuery(query, connection))
            return Result(PixReceivedQueryOutcome.Rejected, "Validation");
        try
        {
            var options = await configuration.ReadAsync(connection, cancellationToken);
            var start = query.Start;
            var end = query.End;
            if (start is null && end is null)
            {
                var days = PixReceivedDefaults.BaseLookbackDays;
                if (options.PixReceivedLookbackDays is { Length: > 0 } configuredDays
                    && (!int.TryParse(configuredDays, NumberStyles.None,
                        CultureInfo.InvariantCulture, out days) || days is < 1 or > 31))
                    throw new IntegrationConfigurationException("PixReceivedLookbackDaysInvalid");
                end = clock.GetUtcNow();
                start = end.Value.AddDays(-days);
            }
            if (start is null || end is null)
                return Result(PixReceivedQueryOutcome.Rejected, "DateRangeIncomplete");

            var offset = checked(query.Page * query.ItemsPerPage);
            var paymentResult = await operations.FindReceivedPaymentsAsync(
                start.Value,
                end.Value,
                offset,
                query.ItemsPerPage,
                connection,
                cancellationToken);
            if (paymentResult.Outcome != AsaasApiOutcome.Completed || paymentResult.Value is null)
                return Result(ToQueryOutcome(paymentResult.Outcome), paymentResult.ErrorCode);

            var transactions = new List<PixReceivedTransaction>();
            var skippedItems = new List<string>();
            foreach (var payment in paymentResult.Value.Items)
            {
                if (!IsAttemptReference(payment.Reference))
                    continue;
                if (payment.PixTransactionId is null)
                {
                    skippedItems.Add("PixTransactionIdUnavailable");
                    continue;
                }

                var transactionResult = await operations.GetPixTransactionAsync(
                    payment.PixTransactionId,
                    connection,
                    cancellationToken);
                if (transactionResult.Outcome != AsaasApiOutcome.Completed || transactionResult.Value is null)
                {
                    skippedItems.Add(transactionResult.ErrorCode ?? "PixTransactionUnavailable");
                    continue;
                }

                var transaction = transactionResult.Value;
                if (!string.Equals(transaction.PaymentId, payment.Id, StringComparison.Ordinal)
                    || transaction.Amount != payment.Amount
                    || transaction.ReceivedAt < start || transaction.ReceivedAt > end)
                {
                    skippedItems.Add("PixTransactionPaymentMismatch");
                    continue;
                }

                transactions.Add(new(
                    transaction.EndToEndId,
                    payment.Reference,
                    transaction.Amount,
                    transaction.ReceivedAt,
                    CombinePayload(payment.ProviderPayloadJson, transaction.ProviderPayloadJson)));
            }

            var totalPages = paymentResult.Value.HasMore ? query.Page + 2 : query.Page + 1;
            var totalItems = paymentResult.Value.HasMore
                ? (query.Page + 1) * query.ItemsPerPage + 1
                : query.Page * query.ItemsPerPage + paymentResult.Value.Items.Count;
            var pagination = new PixReceivedPagination(query.Page, query.ItemsPerPage, totalPages, totalItems);
            if (skippedItems.Count > 0)
            {
                logger.LogWarning(
                    "Asaas Pix received page skipped invalid items and continued. ConnectionId {ConnectionId}; Page {Page}; SkippedItems {SkippedItems}; Reasons {Reasons}",
                    connection.Id,
                    query.Page,
                    skippedItems.Count,
                    string.Join(",", skippedItems.Distinct()));
            }
            return Result(PixReceivedQueryOutcome.Completed, transactions: transactions,
                pagination: pagination, start: start, end: end);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Result(PixReceivedQueryOutcome.Rejected, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TimeoutException
            or System.Text.Json.JsonException or InvalidOperationException or OverflowException)
        {
            logger.LogWarning("Asaas Pix receipt query failed. ConnectionId {ConnectionId}; Page {Page}; ExceptionType {ExceptionType}",
                connection.Id, query.Page, exception.GetType().Name);
            return Result(PixReceivedQueryOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    private static PixReceivedQueryOutcome ToQueryOutcome(AsaasApiOutcome outcome) => outcome switch
    {
        AsaasApiOutcome.Rejected or AsaasApiOutcome.ConfigurationError => PixReceivedQueryOutcome.Rejected,
        _ => PixReceivedQueryOutcome.Indeterminate
    };

    private static bool IsValidQuery(PixReceivedQuery query, ProviderConnection connection) =>
        query.TenantId != Guid.Empty && query.ConnectionId == connection.Id && connection.TenantId == query.TenantId
        && connection.Provider == "Asaas" && connection.Group == "PaymentProvider"
        && string.Equals(query.ExpectedProvider, "Asaas", StringComparison.Ordinal)
        && ((query.Start is null && query.End is null)
            || (query.Start is { } start && query.End is { } end && start <= end))
        && query.TxId is null && query.Page >= 0 && query.ItemsPerPage is > 0 and <= 100;

    private static bool IsAttemptReference(string? reference) => reference is { Length: 32 }
        && reference.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string CombinePayload(string paymentPayload, string transactionPayload) =>
        "{\"payment\":" + paymentPayload + ",\"pixTransaction\":" + transactionPayload + "}";
}
