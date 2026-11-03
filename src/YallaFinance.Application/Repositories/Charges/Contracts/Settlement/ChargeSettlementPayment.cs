using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementPayment(
    Guid TenantId,
    Guid ChargeId,
    Guid Id,
    Guid BankConnectionId,
    Guid PaymentMethodId,
    decimal Amount,
    DateTimeOffset ReceivedAt);
