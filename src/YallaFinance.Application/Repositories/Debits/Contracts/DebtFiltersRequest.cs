using YallaFinance.Domain.Debts;
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record DebtFiltersRequest(
    IReadOnlyList<string>? Categories,
    DebtStatus? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? SortBy,
    string? SortDirection,
    int Page = 1,
    int PageSize = 25);
