using YallaFinance.Infrastructure.Providers.Payments.Pix;
using System.Data.Common;

using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Application.Services.Payments.Pix;

namespace YallaFinance.Infrastructure.Services.Payments.Pix;

internal sealed class PixService(
    ICompanyConnectionRepository connections,
    IProviderResolver<IPixChargeProvider> chargeProviderResolver,
    IProviderResolver<IPixReceivedProvider> receivedProviderResolver) : IPixService
{
    // Prazo para o cliente receber QR Code ou erro na criacao do Pix.
    private static readonly TimeSpan CustomerCreationDeadline = TimeSpan.FromSeconds(15);

    public async Task<CreatePixChargeResult> CreateAsync(
        CreatePixChargeData data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (data is null)
            return new(
                PixChargeCreationOutcome.Rejected,
                Guid.Empty,
                "",
                "",
                ErrorCode: "Validation");
        CreatePixChargeResult Failure(PixChargeCreationOutcome outcome, string error)
            => new(
                outcome,
                data.ConnectionId,
                data.ExpectedProvider,
                data.ProviderReference,
                ErrorCode: error);
        if (!IsValidProviderConnectionIdentity(data.TenantId, data.ConnectionId, data.ExpectedProvider, data.ProviderReference)
            || data.OperationId == Guid.Empty || data.Amount <= 0 || decimal.Round(data.Amount, 2) != data.Amount || data.ExpirationSeconds <= 0)
            return Failure(PixChargeCreationOutcome.Rejected, "Validation");

        var delegated = false;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var connection = await ReadConnectionAsync(
                data.TenantId,
                data.ConnectionId,
                data.ExpectedProvider,
                true,
                cancellationToken);
            var provider = chargeProviderResolver.Resolve(connection.Provider);
            cancellationToken.ThrowIfCancellationRequested();
            delegated = true;
            deadline.CancelAfter(CustomerCreationDeadline);
            return await provider.CreateAsync(data, connection, deadline.Token);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Failure(
                PixChargeCreationOutcome.Rejected,
                exception.Code);
        }
        // Provider contract: cancellation may escape only when creation was not dispatched.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        // Prazo do cliente estourado: erro com nova tentativa; a consulta previa do provider evita duplicidade.
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return Failure(PixChargeCreationOutcome.Rejected, "ProviderDeadlineExceeded");
        }
        catch (Exception exception) when (exception is DbException or IOException or HttpRequestException or TimeoutException or OperationCanceledException)
        {
            return Failure(PixChargeCreationOutcome.Rejected,
                delegated ? "ProviderOutcomeUnknown" : "ConnectionUnavailable");
        }
    }

    public async Task<PixChargeQueryResult> GetAsync(
        PixChargeQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query is null)
            return new(
                PixChargeQueryOutcome.Indeterminate,
                Guid.Empty,
                "",
                "",
                ErrorCode: "Validation");
        PixChargeQueryResult Failure(string error)
            => new(
                PixChargeQueryOutcome.Indeterminate,
                query.ConnectionId,
                query.ExpectedProvider,
                query.ProviderReference,
                ErrorCode: error);
        if (!IsValidProviderConnectionIdentity(
            query.TenantId,
            query.ConnectionId,
            query.ExpectedProvider,
            query.ProviderReference))
            return Failure("Validation");
        try
        {
            var connection = await ReadConnectionAsync(
                query.TenantId,
                query.ConnectionId,
                query.ExpectedProvider,
                false,
                cancellationToken);
            return await chargeProviderResolver.Resolve(connection.Provider).GetAsync(query, connection, cancellationToken);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Failure(exception.Code);
        }
        catch (Exception exception) when (exception is DbException or IOException or HttpRequestException or TimeoutException or OperationCanceledException)
        {
            return Failure("ProviderOutcomeUnknown");
        }
    }

    public async Task<PixReceivedQueryResult> QueryAsync(
        PixReceivedQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query is null)
            return new(
                PixReceivedQueryOutcome.Rejected,
                Guid.Empty,
                string.Empty,
                [],
                ErrorCode: "Validation");
        PixReceivedQueryResult Failure(PixReceivedQueryOutcome outcome, string code)
            => new(outcome, query.ConnectionId, query.ExpectedProvider, [], ErrorCode: code);
        if (!IsValidPixReceivedQuery(query))
            return Failure(PixReceivedQueryOutcome.Rejected, "Validation");

        try
        {
            var connection = await ReadConnectionAsync(
                query.TenantId,
                query.ConnectionId,
                query.ExpectedProvider,
                true,
                cancellationToken);
            if (connection is null || !connection.Enabled || connection.TenantId != query.TenantId || connection.Id != query.ConnectionId
                || connection.Group != "PaymentProvider")
                return Failure(PixReceivedQueryOutcome.Rejected, "ConnectionUnavailable");
            if (!string.Equals(connection.Provider, query.ExpectedProvider, StringComparison.Ordinal))
                return Failure(PixReceivedQueryOutcome.Rejected, "ProviderChanged");
            return await receivedProviderResolver.Resolve(connection.Provider).QueryAsync(
                query,
                connection,
                cancellationToken);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Failure(PixReceivedQueryOutcome.Rejected, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is DbException or IOException or HttpRequestException
            or TimeoutException or OperationCanceledException)
        {
            return Failure(PixReceivedQueryOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    private async Task<ProviderConnection> ReadConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        string expectedProvider,
        bool creating,
        CancellationToken cancellationToken)
    {
        var connectionRecord = await connections.GetRuntimeAsync(tenantId, connectionId, cancellationToken);
        if (connectionRecord is null || connectionRecord.TenantId != tenantId || connectionRecord.Id != connectionId
            || connectionRecord.Group != "PaymentProvider" || (creating && !connectionRecord.Enabled))
            throw new IntegrationConfigurationException("ConnectionUnavailable");
        if (!string.Equals(connectionRecord.Provider, expectedProvider, StringComparison.Ordinal))
            throw new IntegrationConfigurationException("ProviderChanged");
        return new(
            connectionRecord.Id,
            connectionRecord.TenantId,
            connectionRecord.Provider,
            connectionRecord.Name,
            connectionRecord.Enabled,
            connectionRecord.Group);
    }

    private static bool IsValidProviderConnectionIdentity(Guid tenantId, Guid connectionId, string provider, string reference)
        => tenantId != Guid.Empty && connectionId != Guid.Empty && !string.IsNullOrWhiteSpace(provider)
            && !string.IsNullOrWhiteSpace(reference) && !reference.Any(char.IsControl);

    private static bool IsValidPixReceivedQuery(PixReceivedQuery query)
        => query.TenantId != Guid.Empty && query.ConnectionId != Guid.Empty
            && !string.IsNullOrWhiteSpace(query.ExpectedProvider)
            && ((query.Start is null && query.End is null)
                || (query.Start is { } start && query.End is { } end && start != default && end != default && start <= end))
            && (query.TxId is null || (query.TxId.Length is > 0 and <= 35
                && query.TxId.All(char.IsAsciiLetterOrDigit)))
            && query.Page >= 0 && query.ItemsPerPage is > 0 and <= 100;
}
