using MediatR;
using YallaFinance.Application.Common.Results;

using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.ListJobs;

public sealed class ListJobsQueryHandler(JobMonitoringService service) : IRequestHandler<ListJobsQuery, NotificationResult<JobMonitoringPage<JobMonitoringItem>>>
{
    public async Task<NotificationResult<JobMonitoringPage<JobMonitoringItem>>> Handle(
        ListJobsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobStatus = request.Status is null ? (JobStatus?)null : Enum.Parse<JobStatus>(request.Status);
            var jobPage = await service.ListAsync(
                new(
                    request.Key,
                    request.Type,
                    jobStatus,
                    request.SortBy,
                    request.SortDirection,
                    request.StartDate,
                    request.EndDate),
                request.Page,
                request.PageSize,
                cancellationToken);
            return NotificationResult<JobMonitoringPage<JobMonitoringItem>>.Success(jobPage);
        }
        catch (UnauthorizedAccessException)
        {
            return NotificationResult<JobMonitoringPage<JobMonitoringItem>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        }
    }
}
