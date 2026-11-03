using MediatR;
using YallaFinance.Application.Common.Results;

using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.GetJob;

public sealed class GetJobQueryHandler(JobMonitoringService service) : IRequestHandler<GetJobQuery, NotificationResult<JobMonitoringDetail>>
{
    public async Task<NotificationResult<JobMonitoringDetail>> Handle(
        GetJobQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobDetails = await service.GetAsync(Guid.Parse(request.Id), cancellationToken);
            return jobDetails is null ? NotificationResult<JobMonitoringDetail>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Job não encontrado."))
                : NotificationResult<JobMonitoringDetail>.Success(jobDetails);
        }
        catch (UnauthorizedAccessException)
        {
            return NotificationResult<JobMonitoringDetail>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        }
    }
}
