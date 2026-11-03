
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record DebtListResult(
    IReadOnlyList<DebtResponse> Items,
    int TotalItems,
    int Page,
    int PageSize,
    DebtSummary Summary);
