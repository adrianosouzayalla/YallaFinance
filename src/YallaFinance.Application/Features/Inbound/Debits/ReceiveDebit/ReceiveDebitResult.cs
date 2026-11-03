using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ReceiveDebitResult(
    Guid DebtId,
    Guid CustomerId,
    Guid ContractId,
    string ExternalId,
    bool Created);
