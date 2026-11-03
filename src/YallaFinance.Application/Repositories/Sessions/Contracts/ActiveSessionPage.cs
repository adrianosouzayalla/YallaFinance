using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Sessions.Contracts;

public sealed record ActiveSessionPage(IReadOnlyList<ActiveSessionItem> Items, int TotalCount, int Page, int PageSize);
