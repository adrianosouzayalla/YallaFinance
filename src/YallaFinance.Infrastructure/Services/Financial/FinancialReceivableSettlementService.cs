using YallaFinance.Infrastructure.Providers.Financial;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Application.Repositories.Connections;


using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Services.Financial;

namespace YallaFinance.Infrastructure.Services.Financial;

internal sealed class FinancialReceivableSettlementService(
    ICompanyConnectionRepository connections,
    IProviderResolver<IFinancialProvider> resolver) : IFinancialReceivableSettlementService
{
    public async Task<SettleReceivableResult> SettleAsync(
        SettleReceivableData data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsValidSettlementData(data))
            return new(FinancialIntegrationOutcome.Rejected, ErrorCode: "Validation");
        try
        {
            var connection = await ResolveConnectionAsync(data, cancellationToken);
            var provider = ResolveProvider(connection);
            return await provider.SettleAsync(data, connection, cancellationToken);
        }
        catch (IntegrationConfigurationException exception)
        {
            return new(FinancialIntegrationOutcome.Rejected, data.ConnectionId, ErrorCode: exception.Code);
        }
    }

    public async Task<ReceivableSettlementQueryResult> QueryAsync(
        SettleReceivableData data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsValidSettlementData(data))
            return new(ReceivableSettlementState.Rejected, ErrorCode: "Validation");
        try
        {
            var connection = await ResolveConnectionAsync(data, cancellationToken);
            return await ResolveProvider(connection).QuerySettlementAsync(data, connection, cancellationToken);
        }
        catch (IntegrationConfigurationException exception)
        {
            return new(ReceivableSettlementState.Rejected, ErrorCode: exception.Code);
        }
    }

    private async Task<ProviderConnection> ResolveConnectionAsync(
        SettleReceivableData data,
        CancellationToken cancellationToken)
    {
        var connectionRecord = await connections.GetRuntimeAsync(data.TenantId, data.ConnectionId, cancellationToken);
        if (connectionRecord is null || connectionRecord.TenantId != data.TenantId || connectionRecord.Id != data.ConnectionId
            || !connectionRecord.Enabled || connectionRecord.Group != "FinancialSystem")
            throw new IntegrationConfigurationException("ConnectionUnavailable");
        if (connectionRecord.Provider != data.ExpectedProvider)
            throw new IntegrationConfigurationException("ProviderChanged");
        return new(
            connectionRecord.Id,
            connectionRecord.TenantId,
            connectionRecord.Provider,
            connectionRecord.Name,
            connectionRecord.Enabled,
            connectionRecord.Group);
    }

    private IFinancialReceivableSettlementProvider ResolveProvider(ProviderConnection connection)
        => resolver.Resolve(connection.Provider) as IFinancialReceivableSettlementProvider
            ?? throw new IntegrationConfigurationException("OperationNotSupported");

    private static bool IsValidSettlementData(SettleReceivableData? data) => data is not null
        && data.TenantId != Guid.Empty && data.ConnectionId != Guid.Empty && data.ChargeItemId != Guid.Empty
        && data.PaymentTransactionId != Guid.Empty && data.ProviderAccountId != Guid.Empty
        && !string.IsNullOrWhiteSpace(data.ProviderExternalId)
        && !string.IsNullOrWhiteSpace(data.ExpectedProvider) && data.Amount > 0 && decimal.Round(
            data.Amount,
            2) == data.Amount
        && data.ReceiptDate != default && !string.IsNullOrWhiteSpace(data.Identifier) && !data.Identifier.Any(char.IsControl);
}
