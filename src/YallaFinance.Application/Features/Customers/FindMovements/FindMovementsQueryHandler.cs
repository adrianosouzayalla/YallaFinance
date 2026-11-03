using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application.Repositories.Fleet;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Customers.FindMovements;

public sealed class FindMovementsQueryHandler(
    IFleetMovementRepository repository,
    ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<FindMovementsQuery, NotificationResult<IReadOnlyList<FleetMovementResponse>>>
{
    public async Task<NotificationResult<IReadOnlyList<FleetMovementResponse>>> Handle(
        FindMovementsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<IReadOnlyList<FleetMovementResponse>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        return NotificationResult<IReadOnlyList<FleetMovementResponse>>.Success(await repository.FindAsync(
            currentTenant.TenantId,
            request.CustomerId,
            request.OccurrenceDate,
            cancellationToken));
    }
}
