using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.GetCharge;

public sealed class GetChargeQueryHandler(
    IChargeRepository charges,
    ICurrentTenantContext currentTenant,
    ChargeProcessingQueries processing)
    : IRequestHandler<GetChargeQuery, NotificationResult<ChargeDetailsResult>>
{
    public async Task<NotificationResult<ChargeDetailsResult>> Handle(
        GetChargeQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        var charge = await charges.GetAsync(currentTenant.TenantId, request.Id, cancellationToken);
        return charge is null
            ? NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Cobrança não encontrada."))
            : NotificationResult<ChargeDetailsResult>.Success(charge.Items is null ? charge : await processing.EnrichAsync(
                charge,
                cancellationToken));
    }
}
