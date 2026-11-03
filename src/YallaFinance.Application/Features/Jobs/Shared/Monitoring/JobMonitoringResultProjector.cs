using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public static class JobMonitoringResultProjector
{
    private static readonly HashSet<string> Errors = new(StringComparer.Ordinal)
    {
        "InvalidChargePayload", "ChargeNotFound", "InvalidChargeItems", "UnknownJobType", "InvalidJobPayload",
        "JobHandlerError", "InvalidHandlerResult", "ChargeIntegrationPartial", "ChargeIntegrationFailed",
        "Unauthorized", "ProviderResponseInvalid", "ProviderUnavailable", "ProviderRejected",
        "Indeterminate", "MissingFinancialMapping", "FinancialIntegrationFailed",
        "InvalidDebitPayload", "InboundTenantUnavailable", "InboundCategoryUnavailable",
        "InboundCustomerConflict", "InboundContractConflict", "InboundDebitConflict",
        "SettlementPaymentMismatch", "InvalidSettlementCheckpoint", "ChargeSettlementPartial",
        "ChargeSettlementIndeterminate", "ChargeSettlementFailed", "ReceiptConflict", "ReceivableConflict",
        "PaymentConnectionUnavailable", "FinancialConnectionUnavailable", "ProviderChanged",
        "ReceiptAccountMappingMissing", "ReceiptAccountMappingInvalid", "ReceiptTimeZoneMissing",
        "ReceiptTimeZoneInvalid", "SettlementConfigurationInvalid", "InsufficientReceivedAmount"
    };

    public static string Error(string? stored)
    {
        var errorCode = stored?.Split(':', 2)[0];
        return errorCode is not null && Errors.Contains(errorCode) ? errorCode : "JobProcessingFailed";
    }

    public static IReadOnlyList<string> KnownErrorCodes => Errors.OrderBy(
        code => code,
        StringComparer.Ordinal)
        .ToArray();
}
