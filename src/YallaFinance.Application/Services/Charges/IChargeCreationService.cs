using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application;
using YallaFinance.Application.Features.Charges.CreateDirectCharge;

namespace YallaFinance.Application.Services.Charges;

public interface IChargeCreationService
{
    Task<NotificationResult<ChargeDetailsResult>> CreateDirectAsync(string tenantId, CreateDirectChargeCommand request, CancellationToken cancellationToken);
    Task<NotificationResult<ChargeDetailsResult>> CreateAsync(
        string tenantId,
        CreateChargeCommand request,
        CancellationToken cancellationToken);
}
