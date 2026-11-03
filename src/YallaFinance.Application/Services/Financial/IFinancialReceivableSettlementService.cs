using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Services.Financial;

public interface IFinancialReceivableSettlementService
{
    Task<SettleReceivableResult> SettleAsync(SettleReceivableData data, CancellationToken cancellationToken);
    Task<ReceivableSettlementQueryResult> QueryAsync(
        SettleReceivableData data,
        CancellationToken cancellationToken);
}
