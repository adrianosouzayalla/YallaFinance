using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;

public sealed record JobMonitoringFilter(
    string? KeyContains = null,
    string? Type = null,
    JobStatus? Status = null,
    string? SortBy = null,
    string SortDirection = "desc",
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null)
{
    public IReadOnlyDictionary<string, string>? TypeTitles
    {
        get; init;
    }
}
