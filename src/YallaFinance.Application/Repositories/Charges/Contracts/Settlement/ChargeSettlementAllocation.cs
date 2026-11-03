using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementAllocation(
    ChargeSettlementPayment Payment,
    ChargeSettlementItem Item,
    decimal Amount);
