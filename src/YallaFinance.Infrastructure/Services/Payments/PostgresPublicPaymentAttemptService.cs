using Microsoft.Extensions.Logging;

using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Pix;
using YallaFinance.Application.Services.Payments.Boleto;

namespace YallaFinance.Infrastructure.Services.Payments;

public sealed partial class PostgresPublicPaymentAttemptService(
    IPaymentRepository repository,
    IPixService pix,
    TimeProvider clock,
    ILogger<PostgresPublicPaymentAttemptService> logger,
    YallaFinance.Application.Services.Payments.CreditCard.ICreditCardService? cards = null,
    IBoletoService? boletos = null) : IPublicPaymentAttemptService
{
    private const int PixExpirationSeconds = 3600;

    public async Task<NotificationResult<PublicPaymentAttemptResult>> StartOrGetAsync(
        string token,
        string methodCode,
        string idempotencyKey,
        CancellationToken cancellationToken, PublicCardPaymentInput? card = null)
    {
        if (!IsValidPublicPaymentToken(token) || string.IsNullOrWhiteSpace(methodCode) || methodCode.Length > 64
            || string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            return CreatePublicPaymentAttemptFailureResult(ErrorCodes.NotFound, "Solicitação de pagamento não encontrada.");

        CardCheckoutContext? cardContext = null;
        YallaFinance.Application.Services.Payments.CreditCard.Contracts.CreditCardPaymentInput? cardPayment = null;
        if (card is not null)
        {
            cardContext = await repository.GetCardCheckoutContextAsync(token, methodCode.Trim(), cancellationToken);
            if (card.SubmissionId == Guid.Empty || cardContext is null || !card.TryCreatePayment(cardContext.Customer, out cardPayment, out _))
                return CreatePublicPaymentAttemptFailureResult(ErrorCodes.Validation, "Confira os dados do cartão e do titular.");
        }
        var reservation = await repository.ReserveAttemptAsync(
            token,
            methodCode.Trim(),
            idempotencyKey,
            cancellationToken, card?.SubmissionId);

        if (reservation.Error is not null)
            return CreatePublicPaymentAttemptFailureResult(reservation.Error, reservation.Message!);
        if (reservation.Existing is not null)
            return NotificationResult<PublicPaymentAttemptResult>.Success(reservation.Existing);

        var attempt = reservation.Attempt!;
        if (attempt.InstrumentType == YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard && cards is not null)
            return await ProcessCardAttemptAsync(attempt, cardContext, cardPayment, reservation.ReconcileUnknown,
                card?.SameHolderAsCustomer == true, cancellationToken);
        if (attempt.InstrumentType == YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto && boletos is not null)
            return await ProcessBoletoAttemptAsync(attempt, reservation.ReconcileUnknown, cancellationToken);
        if (attempt.InstrumentType != YallaFinance.Domain.Payments.PaymentInstrumentType.Pix)
            return CreatePublicPaymentAttemptFailureResult(ErrorCodes.ServiceUnavailable,
                "A modalidade desta forma de pagamento está indisponível.");
        logger.LogInformation(
            "Payment attempt reserved. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; Status {Status}",
            attempt.Id,
            attempt.ConnectionId,
            attempt.Provider,
            attempt.Status);
        if (reservation.ReconcileUnknown)
            return await ReconcileUnknownAsync(attempt, cancellationToken);

        return await CreatePixChargeAsync(attempt, cancellationToken);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> CreatePixChargeAsync(
        PaymentAttemptData attempt,
        CancellationToken cancellationToken)
    {
        CreatePixChargeResult providerResult;
        try
        {
            PixChargeCreationContext? chargeContext = null;
            try
            {
                chargeContext = await repository.GetPixChargeCreationContextAsync(
                    attempt.TenantId,
                    attempt.ChargeId,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is System.Data.Common.DbException or IOException or TimeoutException)
            {
                logger.LogWarning(
                    "Pix payer context could not be loaded. AttemptId {AttemptId}; ExceptionType {ExceptionType}",
                    attempt.Id,
                    exception.GetType().Name);
            }
            logger.LogInformation(
                "Payment provider call started. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; Method {Method}; ExpirationSeconds {ExpirationSeconds}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                "PIX",
                PixExpirationSeconds);
            providerResult = await pix.CreateAsync(new CreatePixChargeData(
                attempt.TenantId,
                attempt.ConnectionId,
                attempt.Provider,
                attempt.Id,
                attempt.ProviderExternalId,
                attempt.Amount,
                PixExpirationSeconds,
                chargeContext?.Payer,
                DueDate: chargeContext?.DueDate), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The provider integration contract only lets cancellation escape before dispatch.
            logger.LogWarning(
                "Payment provider call canceled before dispatch. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; FailureCode {FailureCode}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                "ProviderNotDispatched");
            await PersistRejectedAsync(attempt.Id, "ProviderNotDispatched", CancellationToken.None);
            return NotificationResult<PublicPaymentAttemptResult>.Success(new(
                "Failed",
                attempt.Amount,
                null,
                null,
                null,
                true,
                "A emissão não foi iniciada. Você pode tentar novamente."));
        }
        catch (Exception exception)
        {
            var failureCode = ProviderCallFailureCode(exception);
            logger.LogError(
                "Payment provider call threw an unexpected exception. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; Stage {Stage}; FailureCode {FailureCode}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}; HResult {HResult}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                "ProviderCall",
                failureCode,
                exception.GetType().Name,
                exception.GetBaseException().GetType().Name,
                exception.HResult);
            await PersistUnknownAsync(attempt.Id, failureCode, CancellationToken.None);
            return NotificationResult<PublicPaymentAttemptResult>.Success(CreatePublicPaymentAttemptResult(
                attempt,
                "Unknown",
                false,
                "Não foi possível confirmar o pagamento. Tente novamente."));
        }

        // Once the provider has returned, client disconnects must not cancel durable result storage.
        logger.LogInformation(
            "Payment provider call returned. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; ProviderOutcome {ProviderOutcome}; FailureCode {FailureCode}; HasCharge {HasCharge}",
            attempt.Id,
            attempt.ConnectionId,
            attempt.Provider,
            providerResult.Outcome,
            SafeFailureCode(providerResult.ErrorCode) ?? "None",
            providerResult.Charge is not null);
        return await PersistProviderResultAsync(attempt, providerResult, CancellationToken.None);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> ReconcileUnknownAsync(
        PaymentAttemptData attempt,
        CancellationToken cancellationToken)
    {
        PixChargeQueryResult query;
        try
        {
            logger.LogInformation(
                "Payment provider query started for unknown attempt. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider);
            query = await pix.GetAsync(new PixChargeQuery(
                attempt.TenantId,
                attempt.ConnectionId,
                attempt.Provider,
                attempt.ProviderExternalId,
                new DateTimeOffset(DateTime.SpecifyKind(attempt.CreatedAt, DateTimeKind.Utc))),
                cancellationToken);
        }
        catch (Exception exception)
        {
            var failureCode = ProviderCallFailureCode(exception);
            logger.LogError(
                "Payment provider query threw an unexpected exception. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; FailureCode {FailureCode}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}; HResult {HResult}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                failureCode,
                exception.GetType().Name,
                exception.GetBaseException().GetType().Name,
                exception.HResult);
            await PersistUnknownAsync(attempt.Id, failureCode, CancellationToken.None);
            return CreateUnknownPublicPaymentAttemptResult(attempt);
        }

        logger.LogInformation(
            "Payment provider query returned for unknown attempt. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; Outcome {ProviderOutcome}; FailureCode {FailureCode}; HasCharge {HasCharge}",
            attempt.Id,
            attempt.ConnectionId,
            attempt.Provider,
            query.Outcome,
            SafeFailureCode(query.ErrorCode) ?? "None",
            query.Charge is not null);
        if (query.ConnectionId != attempt.ConnectionId
            || !string.Equals(query.Provider, attempt.Provider, StringComparison.Ordinal)
            || !string.Equals(query.ProviderReference, attempt.ProviderExternalId, StringComparison.Ordinal))
            return await PersistUnknownPaymentAttemptAndReturnAsync(
                attempt,
                "ProviderQueryIdentityMismatch",
                CancellationToken.None);

        if (query.Outcome == PixChargeQueryOutcome.Found)
        {
            var found = new CreatePixChargeResult(
                PixChargeCreationOutcome.Completed,
                query.ConnectionId,
                query.Provider,
                query.ProviderReference,
                query.Charge,
                query.ErrorCode);
            return await PersistProviderResultAsync(attempt, found, CancellationToken.None);
        }

        if (query.Outcome != PixChargeQueryOutcome.NotFound)
            return await PersistUnknownPaymentAttemptAndReturnAsync(
                attempt,
                SafeFailureCode(query.ErrorCode) ?? "ProviderQueryIndeterminate",
                CancellationToken.None,
                query.FailureMessage);

        var requestCanReceivePayment = await repository.IsPaymentRequestActiveAsync(
            attempt.TenantId,
            attempt.PaymentRequestId,
            CancellationToken.None);
        if (!requestCanReceivePayment)
        {
            await PersistProviderNotFoundAsync(attempt.Id, CancellationToken.None);
            return NotificationResult<PublicPaymentAttemptResult>.Success(new(
                "Failed",
                attempt.Amount,
                null,
                null,
                null,
                false,
                "Esta solicitação de pagamento não está mais disponível."));
        }

        logger.LogInformation(
            "Payment provider confirmed Pix was not created; creating the charge for the same attempt and provider reference. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}",
            attempt.Id,
            attempt.ConnectionId,
            attempt.Provider);
        return await CreatePixChargeAsync(attempt, cancellationToken);
    }

    private static NotificationResult<PublicPaymentAttemptResult> CreateUnknownPublicPaymentAttemptResult(PaymentAttemptData attempt) =>
        NotificationResult<PublicPaymentAttemptResult>.Success(CreatePublicPaymentAttemptResult(attempt,
            "Unknown", false, "Não foi possível confirmar o pagamento. Tente novamente."));

    private async Task<NotificationResult<PublicPaymentAttemptResult>> PersistProviderResultAsync(
        PaymentAttemptData attempt,
        CreatePixChargeResult result,
        CancellationToken ct)
    {
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        var identityMatches = result.ConnectionId == attempt.ConnectionId
            && string.Equals(result.Provider, attempt.Provider, StringComparison.Ordinal)
            && string.Equals(result.ProviderReference, attempt.ProviderExternalId, StringComparison.Ordinal);
        if (!identityMatches)
        {
            logger.LogWarning(
                "Payment provider result identity mismatch. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; FailureCode {FailureCode}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                "ProviderIdentityMismatch");
            return await PersistUnknownPaymentAttemptAndReturnAsync(attempt, "ProviderIdentityMismatch", ct);
        }

        if (result.Outcome == PixChargeCreationOutcome.Rejected)
        {
            var failureCode = SafeFailureCode(result.ErrorCode) ?? "ProviderRejected";
            logger.LogWarning(
                "Payment provider rejected Pix creation. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; FailureCode {FailureCode}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                failureCode);
            await PersistRejectedAsync(attempt.Id, failureCode, ct, result.FailureMessage);
            return NotificationResult<PublicPaymentAttemptResult>.Success(new(
                "Failed",
                attempt.Amount,
                null,
                null,
                null,
                true,
                "Não foi possível gerar o Pix. Você pode tentar novamente."));
        }

        if (!PaymentAttemptProviderResultValidator.TryGetPayableSnapshot(
            attempt,
            result,
            currentUtcTime,
            out var snapshot,
            out var validationFailureCode))
        {
            var failureCode = SafeFailureCode(result.ErrorCode) ?? validationFailureCode;
            logger.LogWarning(
                "Payment provider response failed payable validation. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; FailureCode {FailureCode}",
                attempt.Id,
                attempt.ConnectionId,
                attempt.Provider,
                failureCode);
            return await PersistUnknownPaymentAttemptAndReturnAsync(attempt, failureCode, ct, result.FailureMessage);
        }

        await repository.PersistPixInstrumentAsync(attempt.TenantId, attempt.Id, snapshot, ct);
        logger.LogInformation(
            "Payment attempt Pix instrument persisted. AttemptId {AttemptId}; ConnectionId {ConnectionId}; Provider {Provider}; Status {Status}",
            attempt.Id,
            attempt.ConnectionId,
            attempt.Provider,
            "AwaitingPayment");
        return NotificationResult<PublicPaymentAttemptResult>.Success(new(
            "AwaitingPayment",
            attempt.Amount,
            snapshot.ExpiresAt.UtcDateTime,
            snapshot.CopyPaste,
            snapshot.Location,
            false,
            null));
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> PersistUnknownPaymentAttemptAndReturnAsync(
        PaymentAttemptData attempt,
        string failureCode,
        CancellationToken ct,
        string? failureMessage = null)
    {
        await PersistUnknownAsync(attempt.Id, failureCode, ct, failureMessage);
        return NotificationResult<PublicPaymentAttemptResult>.Success(new("Unknown", attempt.Amount, null,
            null, null, false, "Não foi possível confirmar o pagamento. Tente novamente."));
    }

    private async Task PersistUnknownAsync(Guid attemptId, string code, CancellationToken cancellationToken,
        string? failureMessage = null)
    {
        var persisted = await repository.MarkAttemptUnknownAsync(attemptId, code, cancellationToken, failureMessage);
        logger.LogWarning(
            "Payment attempt outcome persisted as unknown. AttemptId {AttemptId}; FailureCode {FailureCode}; Persisted {Persisted}",
            attemptId,
            SafeFailureCode(code) ?? "ProviderOutcomeUnknown",
            persisted);
    }
    private async Task PersistProviderNotFoundAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var persisted = await repository.MarkAttemptProviderNotFoundAsync(attemptId, cancellationToken);
        logger.LogInformation(
            "Payment attempt closed after provider confirmed no Pix charge exists. AttemptId {AttemptId}; Persisted {Persisted}",
            attemptId,
            persisted);
    }
    private async Task PersistRejectedAsync(Guid attemptId, string code, CancellationToken cancellationToken,
        string? failureMessage = null)
    {
        var persisted = await repository.MarkAttemptRejectedAsync(attemptId, code, cancellationToken, failureMessage);
        logger.LogInformation(
            "Payment attempt failure persisted. AttemptId {AttemptId}; FailureCode {FailureCode}; Persisted {Persisted}",
            attemptId,
            SafeFailureCode(code) ?? "ProviderRejected",
            persisted);
    }
    private static PublicPaymentAttemptResult CreatePublicPaymentAttemptResult(
        PaymentAttemptData attempt,
        string? status = null,
        bool? canRetry = null,
        string? message = null) => new(
            status ?? attempt.Status,
            attempt.Amount,
            attempt.ExpiresAt,
            null,
            null,
            canRetry ?? (attempt.Status == "Failed" && !attempt.IssuanceBlocked),
            message);
    private static bool IsValidPublicPaymentToken(string? token) => token is { Length: 64 }
        && token.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace(
        '+',
        '-')
        .Replace(
            '/',
            '_');

    private static string? SafeFailureCode(string? code) => code is { Length: > 0 and <= 80 }
        && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-') ? code : null;

    private static string ProviderCallFailureCode(Exception exception)
    {
        static string SafeType(Exception value)
        {
            var name = new string(value.GetType().Name.Where(char.IsAsciiLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }

        var root = exception.GetBaseException();
        return SafeFailureCode($"ProviderCallFailure_{SafeType(exception)}_{SafeType(root)}") ?? "ProviderOutcomeUnknown";
    }

    private static NotificationResult<PublicPaymentAttemptResult> CreatePublicPaymentAttemptFailureResult(
        string code,
        string message) =>
        NotificationResult<PublicPaymentAttemptResult>.Failure(new NotificationError(code, message));

}
