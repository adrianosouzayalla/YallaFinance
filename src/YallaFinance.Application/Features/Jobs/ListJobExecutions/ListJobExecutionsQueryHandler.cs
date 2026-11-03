using MediatR;
using YallaFinance.Application.Common.Results;

using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.ListJobExecutions;

public sealed class ListJobExecutionsQueryHandler(JobMonitoringService service) : IRequestHandler<ListJobExecutionsQuery, NotificationResult<JobMonitoringExecutionPage>>
{
    public async Task<NotificationResult<JobMonitoringExecutionPage>> Handle(
        ListJobExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var executionPage = await service.ListExecutionsAsync(Guid.Parse(request.Id), request.Page, request.PageSize, cancellationToken,
                request.SortBy, request.SortDirection);
            return executionPage is null ? NotificationResult<JobMonitoringExecutionPage>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Job não encontrado."))
                : NotificationResult<JobMonitoringExecutionPage>.Success(executionPage);
        }
        catch (UnauthorizedAccessException)
        {
            return NotificationResult<JobMonitoringExecutionPage>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        }
    }
}
