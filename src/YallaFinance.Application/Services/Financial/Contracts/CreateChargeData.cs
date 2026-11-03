using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record CreateChargeData(
    Guid ChargeId,
    Guid TenantId,
    FinancialChargeCustomer Customer,
    Guid CategoryId,
    decimal Amount,
    DateOnly ScheduleDate,
    DateOnly DueDate,
    DateOnly AccrualDate,
    string Description,
    Guid? ConnectionId = null,
    string? ExpectedProvider = null,
    string? ExternalCostCenterId = null);
