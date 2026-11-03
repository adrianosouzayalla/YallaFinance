using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record CreateReceivableResult(
    FinancialIntegrationOutcome Outcome,
    string? ProviderExternalId,
    string? ErrorCode,
    FinancialProviderDiagnostic? Diagnostic = null)
{
    public bool Success => Outcome == FinancialIntegrationOutcome.Completed;
    public static CreateReceivableResult Completed(string externalId) => new(
        FinancialIntegrationOutcome.Completed,
        externalId,
        null);
    public static CreateReceivableResult Rejected(string code, FinancialProviderDiagnostic? diagnostic = null) => new(
        FinancialIntegrationOutcome.Rejected,
        null,
        code,
        diagnostic);
    public static CreateReceivableResult Indeterminate(FinancialProviderDiagnostic? diagnostic = null) => new(
        FinancialIntegrationOutcome.Indeterminate,
        null,
        "ProviderOutcomeUnknown",
        diagnostic);
}
