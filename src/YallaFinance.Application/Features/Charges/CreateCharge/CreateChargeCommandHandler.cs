using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed class CreateChargeCommandHandler(
    ICurrentTenantContext currentTenant,
    IChargeCreationService charges)
    : MediatR.IRequestHandler<CreateChargeCommand, NotificationResult<ChargeDetailsResult>>
{
    public Task<NotificationResult<ChargeDetailsResult>> Handle(
        CreateChargeCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return Task.FromResult(NotificationResult<ChargeDetailsResult>.Failure(
                new NotificationError(ErrorCodes.Unauthorized, "Sessão sem tenant ativo.")));
        return charges.CreateAsync(currentTenant.TenantId, request, cancellationToken);
    }
}