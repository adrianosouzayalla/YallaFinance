using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ListJobExecutions;

public sealed record ListJobExecutionsQuery(
    string Id,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortDirection = "asc") : IRequest<NotificationResult<JobMonitoringExecutionPage>>;
