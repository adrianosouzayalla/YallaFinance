using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementSnapshot(
    Guid TenantId,
    Guid ChargeId,
    decimal Amount,
    IReadOnlyList<ChargeSettlementItem> Items,
    IReadOnlyList<ChargeSettlementPayment> Payments,
    bool IsSettled = false);
