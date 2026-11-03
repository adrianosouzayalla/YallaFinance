using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.ReprocessJob;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public sealed class ReprocessJobCommandHandler(JobReprocessingService service)
    : IRequestHandler<ReprocessJobCommand, NotificationResult<JobReprocessingReceipt>>
{
    public Task<NotificationResult<JobReprocessingReceipt>> Handle(
        ReprocessJobCommand request,
        CancellationToken cancellationToken) =>
        service.ReprocessAsync(Guid.Parse(request.Id), cancellationToken);
}
