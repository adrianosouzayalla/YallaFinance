using YallaFinance.Infrastructure.Providers.Configuration;

using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Infrastructure.Providers;

namespace YallaFinance.Infrastructure.Providers.Financial;

internal interface IFinancialReceivableSettlementProvider : IProviderCapability
{
    Task<Guid> ResolveReceiptAccountAsync(
        ChargeSettlementPayment payment,
        ProviderConnection connection,
        CancellationToken cancellationToken);
    Task<SettleReceivableResult> SettleAsync(
        SettleReceivableData data,
        ProviderConnection connection,
        CancellationToken cancellationToken);
    Task<ReceivableSettlementQueryResult> QuerySettlementAsync(
        SettleReceivableData data,
        ProviderConnection connection,
        CancellationToken cancellationToken);
}
