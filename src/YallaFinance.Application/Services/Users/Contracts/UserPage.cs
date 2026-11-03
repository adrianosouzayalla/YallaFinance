using YallaFinance.Application;
namespace YallaFinance.Application.Services.Users.Contracts;

public sealed record UserPage(IReadOnlyList<UserDetail> Items, int TotalCount, int Page, int PageSize);
