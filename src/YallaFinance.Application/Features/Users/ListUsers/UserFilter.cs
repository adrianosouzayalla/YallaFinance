using YallaFinance.Application;
namespace YallaFinance.Application.Features.Users.ListUsers;

public sealed record UserFilter(string? Search = null, bool? UserActive = null, bool? AccessActive = null,
    int Page = 1, int PageSize = 20, string SortBy = "name", string SortDirection = "asc");
