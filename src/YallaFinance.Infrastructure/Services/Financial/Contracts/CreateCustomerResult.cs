using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record CreateCustomerResult(
    FinancialIntegrationOutcome Outcome,
    string? ProviderExternalId,
    string? ErrorCode,
    FinancialProviderDiagnostic? Diagnostic = null)
{
    public bool Success => Outcome == FinancialIntegrationOutcome.Completed;
    public static CreateCustomerResult Completed(string externalId) => new(
        FinancialIntegrationOutcome.Completed,
        externalId,
        null);
    public static CreateCustomerResult Rejected(string code, FinancialProviderDiagnostic? diagnostic = null) => new(
        FinancialIntegrationOutcome.Rejected,
        null,
        code,
        diagnostic);
    public static CreateCustomerResult Indeterminate(FinancialProviderDiagnostic? diagnostic = null) => new(
        FinancialIntegrationOutcome.Indeterminate,
        null,
        "ProviderOutcomeUnknown",
        diagnostic);
}
