using YallaFinance.Application;
namespace YallaFinance.Application.Features.Debits.CreateManualDebit;

public sealed record CreateManualDebitResult(Guid DebtId, bool Created);
