using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ListJobTypes;

public sealed class ListJobTypesQueryHandler(JobMonitoringService service)
    : IRequestHandler<ListJobTypesQuery, NotificationResult<IReadOnlyList<JobMonitoringType>>>
{
    public async Task<NotificationResult<IReadOnlyList<JobMonitoringType>>> Handle(
        ListJobTypesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            return NotificationResult<IReadOnlyList<JobMonitoringType>>.Success(await service.ListTypesAsync(cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return NotificationResult<IReadOnlyList<JobMonitoringType>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        }
    }
}
