using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ListJobs;

// Optional creation instants: StartDate inclusive, EndDate exclusive. Clients convert
// their local calendar range to UTC, including midnight after the selected final day.
public sealed record ListJobsQuery(
    string? Key = null,
    string? Type = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortDirection = "desc",
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null) : IRequest<NotificationResult<JobMonitoringPage<JobMonitoringItem>>>;
