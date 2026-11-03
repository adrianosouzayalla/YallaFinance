using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record CreateChargeResult(
    FinancialIntegrationOutcome Outcome,
    string? ProviderExternalId,
    string? ProviderCustomerId,
    Guid? ConnectionId,
    bool AlreadyExisted,
    string? ErrorCode,
    string? Provider = null,
    FinancialProviderDiagnostic? Diagnostic = null)
{
    public bool Success => Outcome == FinancialIntegrationOutcome.Completed;
    public static CreateChargeResult Completed(
        string externalId,
        string customerId,
        Guid connectionId,
        bool alreadyExisted = false)
        => new(
            FinancialIntegrationOutcome.Completed,
            externalId,
            customerId,
            connectionId,
            alreadyExisted,
            null);
    public static CreateChargeResult Rejected(
        string code,
        Guid? connectionId = null,
        string? customerId = null,
        FinancialProviderDiagnostic? diagnostic = null)
        => new(FinancialIntegrationOutcome.Rejected, null, customerId, connectionId, false, code, Diagnostic: diagnostic);
    public static CreateChargeResult Indeterminate(
        Guid connectionId,
        string? customerId = null,
        string code = "ProviderOutcomeUnknown",
        FinancialProviderDiagnostic? diagnostic = null)
        => new(FinancialIntegrationOutcome.Indeterminate, null, customerId, connectionId, false, code, Diagnostic: diagnostic);
}
