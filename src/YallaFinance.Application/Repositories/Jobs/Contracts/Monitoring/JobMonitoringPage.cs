using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;

public sealed record JobMonitoringPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    JobMonitoringSummary Summary);
