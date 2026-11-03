using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application;
namespace YallaFinance.Application.Services.Charges;

public interface IChargeSettlementConfiguration
{
    Task<ChargeSettlementTarget> ResolveAsync(
        ChargeSettlementPayment payment,
        Guid? preferredConnectionId,
        CancellationToken cancellationToken);

    Task<Guid> ResolveReceiptAccountAsync(
        ChargeSettlementPayment payment,
        Guid connectionId,
        CancellationToken cancellationToken);
}
