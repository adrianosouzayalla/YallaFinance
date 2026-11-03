
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Fleet.Contracts;

public sealed record FleetMovementResponse(
    string Id,
    string TenantId,
    DateOnly OccurrenceDate,
    string Plate,
    string Contract,
    string Product,
    string Fleet,
    string CostCenter,
    string? CustomerId = null);
