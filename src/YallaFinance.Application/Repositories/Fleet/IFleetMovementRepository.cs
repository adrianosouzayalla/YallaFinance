using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Fleet;

public interface IFleetMovementRepository
{
    Task<IReadOnlyList<FleetMovementResponse>> FindAsync(
        string tenantId,
        string customerId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken);
}
