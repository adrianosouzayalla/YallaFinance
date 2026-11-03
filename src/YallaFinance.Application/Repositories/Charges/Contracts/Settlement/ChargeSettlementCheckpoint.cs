using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementCheckpoint(
    int SchemaVersion,
    int Revision,
    Guid ChargeId,
    Guid PaymentTransactionId,
    Guid BankConnectionId,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<ChargeSettlementItemCheckpoint> Items,
    IReadOnlyList<ChargeSettlementPayment>? Payments = null);
