using YallaFinance.Application;
namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed record ChargeIntegrationItemResult(
    Guid ChargeItemId,
    string Outcome,
    bool AlreadyLinked = false,
    string? ErrorCode = null,
    Guid? ConnectionId = null,
    YallaFinance.Application.Services.Financial.Contracts.FinancialProviderDiagnostic? Diagnostic = null);

