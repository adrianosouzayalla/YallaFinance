using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs.Contracts;

public sealed record JobPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
