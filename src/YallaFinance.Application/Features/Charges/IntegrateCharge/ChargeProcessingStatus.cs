using YallaFinance.Application;
namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed record ChargeProcessingStatus(
    Guid? JobId,
    string Status,
    ChargeIntegrationSummary? Result = null,
    string? ErrorCode = null);
