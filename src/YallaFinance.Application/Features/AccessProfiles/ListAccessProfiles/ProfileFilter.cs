using YallaFinance.Application;
namespace YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;

public sealed record ProfileFilter(string? Search = null, bool? Active = null, int Page = 1,
    int PageSize = 20, string SortBy = "name", string SortDirection = "asc");
