using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;

namespace YallaFinance.Application.Repositories.Payments;

public interface IPaymentRepository
{
    Task<CardCheckoutContext?> GetCardCheckoutContextAsync(string token, string methodCode, CancellationToken cancellationToken);
    Task<CardAttemptState?> GetCardAttemptAsync(Guid tenantId, Guid attemptId, CancellationToken cancellationToken);
    Task<BoletoAttemptState?> GetBoletoAttemptAsync(Guid tenantId, Guid attemptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CardPaymentReconciliationCandidate>> ListCardPaymentReconciliationCandidatesAsync(
        Guid tenantId,
        Guid connectionId,
        int limit,
        int offset,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<BoletoPaymentReconciliationCandidate>> ListBoletoPaymentReconciliationCandidatesAsync(
        Guid tenantId,
        Guid connectionId,
        int limit,
        int offset,
        CancellationToken cancellationToken);
    Task SaveCardIdentityAsync(PaymentAttemptData attempt, string? customerId, string? chargeId, string? providerStatus, CancellationToken cancellationToken);
    Task<bool> TryMarkCardDispatchedAsync(PaymentAttemptData attempt, CancellationToken cancellationToken);
    Task PersistCardResultAsync(PaymentAttemptData attempt, CreditCardCharge charge, CancellationToken cancellationToken);
    Task PersistBoletoResultAsync(PaymentAttemptData attempt, BoletoCharge charge, CancellationToken cancellationToken);
    Task<CardAttemptFailureResult> RecordCardAttemptFailureAsync(
        PaymentAttemptData attempt,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken);
    Task<PaymentChargeSnapshot?> GetChargeAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken);

    Task<PixChargeCreationContext?> GetPixChargeCreationContextAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken);

    Task<PaymentChargeCreationContext?> GetPaymentChargeCreationContextAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken);

    Task<PaymentChargeListResult> ListAsync(
        Guid tenantId,
        PaymentChargeListFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentMethodOption>> AvailableMethodsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<NotificationResult<PaymentRequestCreated>> PublishAsync(
        Guid tenantId,
        PublishPaymentRequestData data,
        CancellationToken cancellationToken);

    Task<NotificationResult<PaymentLinkResult>> RecoverLinkAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<NotificationResult<PaymentRequestCreated>> GetAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<NotificationResult<PublicPaymentCheckout>> GetPublicCheckoutAsync(
        string token,
        CancellationToken cancellationToken);

    Task<NotificationResult<PaymentRequestCreated>> CancelAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<NotificationResult<PaymentReceiptPersistenceResult>> PersistReceivedReceiptsAsync(
        Guid tenantId,
        Guid connectionId,
        string provider,
        IReadOnlyList<PixReceivedTransaction> receipts,
        CancellationToken cancellationToken);

    Task<PaymentAttemptReservation> ReserveAttemptAsync(
        string token,
        string methodCode,
        string idempotencyKey,
        CancellationToken cancellationToken, Guid? cardSubmissionId = null);

    Task<bool> IsPaymentRequestActiveAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<bool> PersistPixInstrumentAsync(
        Guid tenantId,
        Guid attemptId,
        PixChargeSnapshot snapshot,
        CancellationToken cancellationToken);

    Task<bool> MarkAttemptUnknownAsync(Guid attemptId, string failureCode, CancellationToken cancellationToken,
        string? failureMessage = null);

    Task<bool> MarkAttemptProviderNotFoundAsync(Guid attemptId, CancellationToken cancellationToken);

    Task<bool> MarkAttemptRejectedAsync(Guid attemptId, string failureCode, CancellationToken cancellationToken,
        string? failureMessage = null);
}

public sealed record CardCheckoutContext(Guid TenantId, Guid ChargeId, DateOnly DueDate, CustomerProfileData Customer);
public sealed record CardAttemptState(string? CustomerId, string? ChargeId, string? ProviderStatus, DateTime? PaymentDispatchedAt);
public sealed record CardAttemptFailureResult(string Status, bool CanRetry, string? Message);
public sealed record CardPaymentReconciliationCandidate(PaymentAttemptData Attempt, string ProviderChargeId);
public sealed record BoletoAttemptState(string? ProviderPaymentId, string? ProviderStatus);
public sealed record BoletoPaymentReconciliationCandidate(PaymentAttemptData Attempt, string? ProviderPaymentId);

public sealed record PaymentAttemptData(
    Guid Id,
    Guid TenantId,
    Guid ChargeId,
    Guid PaymentRequestId,
    Guid PaymentMethodId,
    Guid ConnectionId,
    string Provider,
    string ProviderExternalId,
    decimal Amount,
    string Status,
    bool IssuanceBlocked,
    DateTime? UpdatedAt,
    DateTime? ExpiresAt)
{
    public YallaFinance.Domain.Payments.PaymentInstrumentType? InstrumentType { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record PixChargeCreationContext(DateOnly DueDate, PixChargePayer? Payer);

public sealed record PaymentAttemptReservation(
    PaymentAttemptData? Attempt,
    PublicPaymentAttemptResult? Existing,
    string? Error,
    string? Message,
    bool ReconcileUnknown = false,
    bool RetryReservation = false)
{
    public static PaymentAttemptReservation New(PaymentAttemptData attempt) => new(attempt, null, null, null);

    public static PaymentAttemptReservation Recover(PaymentAttemptData attempt) => new(
        attempt,
        null,
        null,
        null,
        ReconcileUnknown: true);

    public static PaymentAttemptReservation RetryAfterRiskClosure() => new(
        null,
        null,
        null,
        null,
        RetryReservation: true);

    public static PaymentAttemptReservation Replay(PublicPaymentAttemptResult result) => new(
        null,
        result,
        null,
        null);

    public static PaymentAttemptReservation Fail(string code, string message) => new(
        null,
        null,
        code,
        message);
}
