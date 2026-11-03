using MediatR;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed record ChargeCreatedNotification(string TenantId, string ChargeId) : INotification
{
    public ChargeProcessingStatus? Processing
    {
        get; set;
    }
}
