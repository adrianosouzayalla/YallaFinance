using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs.Contracts;

public sealed record JobListFilter(string? Key = null, string? Type = null);
