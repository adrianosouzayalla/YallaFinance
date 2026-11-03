
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Services.Payments;

internal static class PaymentAttemptProviderResultValidator
{
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    public static bool TryGetPayableSnapshot(
        PaymentAttemptData attempt,
        CreatePixChargeResult result,
        DateTime nowUtc,
        out PixChargeSnapshot snapshot,
        out string failureCode)
    {
        snapshot = null!;
        failureCode = "ProviderResultInvalid";
        var candidate = result.Charge;
        if (result.Outcome != PixChargeCreationOutcome.Completed)
            return SetFailureCodeAndReturnFalse(
                "ProviderOutcomeNotCompleted",
                out failureCode);
        if (candidate is null)
            return SetFailureCodeAndReturnFalse("ProviderChargeMissing", out failureCode);
        if (result.ConnectionId != attempt.ConnectionId)
            return SetFailureCodeAndReturnFalse(
                "ProviderConnectionMismatch",
                out failureCode);
        if (!string.Equals(result.Provider, attempt.Provider, StringComparison.Ordinal))
            return SetFailureCodeAndReturnFalse(
                "ProviderNameMismatch",
                out failureCode);
        if (!string.Equals(result.ProviderReference, attempt.ProviderExternalId, StringComparison.Ordinal)
            || !string.Equals(
                candidate.ProviderReference,
                attempt.ProviderExternalId,
                StringComparison.Ordinal))
            return SetFailureCodeAndReturnFalse("ProviderReferenceMismatch", out failureCode);
        if (candidate.Amount != attempt.Amount)
            return SetFailureCodeAndReturnFalse("ProviderAmountMismatch", out failureCode);
        if (candidate.Status != PixChargeStatus.Active)
            return SetFailureCodeAndReturnFalse(
                "ProviderChargeNotActive",
                out failureCode);
        if (string.IsNullOrWhiteSpace(candidate.CopyPaste))
            return SetFailureCodeAndReturnFalse(
                "ProviderPixCodeMissing",
                out failureCode);
        if (candidate.CreatedAt.UtcDateTime > nowUtc.Add(ClockTolerance))
            return SetFailureCodeAndReturnFalse(
                "ProviderCreatedAtInFuture",
                out failureCode);
        if (candidate.ExpiresAt.UtcDateTime <= nowUtc)
            return SetFailureCodeAndReturnFalse("ProviderChargeExpired", out failureCode);
        if (candidate.ExpiresAt <= candidate.CreatedAt)
            return SetFailureCodeAndReturnFalse(
                "ProviderInvalidExpiration",
                out failureCode);
        if (candidate.ExpiresAt <= candidate.ObservedAt)
            return SetFailureCodeAndReturnFalse(
                "ProviderObservedAfterExpiration",
                out failureCode);

        snapshot = candidate;
        failureCode = string.Empty;
        return true;
    }

    private static bool SetFailureCodeAndReturnFalse(string code, out string failureCode)
    {
        failureCode = code;
        return false;
    }
}
