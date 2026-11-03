using MediatR;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed class ChargeCreatedNotificationHandler(JobService jobs)
    : INotificationHandler<ChargeCreatedNotification>
{
    public async Task Handle(ChargeCreatedNotification notification, CancellationToken cancellationToken)
    {
        var jobId = await jobs.CreateAsync(Guid.Parse(notification.TenantId),
            new IntegrateChargeJob(Guid.Parse(notification.ChargeId)), cancellationToken);
        notification.Processing = new(jobId, "Pending");
    }
}
