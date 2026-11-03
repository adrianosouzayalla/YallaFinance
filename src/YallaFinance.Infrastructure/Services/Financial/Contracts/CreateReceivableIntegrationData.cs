namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record CreateReceivableIntegrationData(
    Guid ChargeId,
    Guid TenantId,
    Guid ProviderCustomerId,
    Guid CategoryId,
    decimal Amount,
    DateOnly ScheduleDate,
    DateOnly DueDate,
    DateOnly AccrualDate,
    string Description,
    string Provider,
    Guid? ConnectionId = null,
    string? ExternalCostCenterId = null);
