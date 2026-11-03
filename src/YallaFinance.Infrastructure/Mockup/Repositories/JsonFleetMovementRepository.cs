


using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application.Repositories.Fleet;

namespace YallaFinance.Infrastructure.Mockup.Repositories;

public sealed class JsonFleetMovementRepository(MockSeedData seed) : IFleetMovementRepository
{
    public Task<IReadOnlyList<FleetMovementResponse>> FindAsync(
        string tenantId,
        string customerId,
        DateOnly occurrenceDate,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FleetMovementResponse>>(seed.FleetMovements.Where(item => item.TenantId == tenantId &&
            item.CustomerId == customerId &&
            item.OccurrenceDate == occurrenceDate)
            .ToArray());
}
