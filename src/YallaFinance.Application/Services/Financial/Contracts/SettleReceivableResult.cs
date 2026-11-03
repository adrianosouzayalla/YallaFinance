using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record SettleReceivableResult(
    FinancialIntegrationOutcome Outcome,
    Guid? ConnectionId = null,
    string? ProviderReceiptId = null,
    bool AlreadyCompleted = false,
    string? ErrorCode = null,
    FinancialProviderDiagnostic? Diagnostic = null)
{
    public bool Success => Outcome == FinancialIntegrationOutcome.Completed;
}
