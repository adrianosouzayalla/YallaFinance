using Microsoft.Extensions.Logging;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;

namespace YallaFinance.Infrastructure.Services.Payments;

public sealed partial class PostgresPublicPaymentAttemptService
{
    private async Task<NotificationResult<PublicPaymentAttemptResult>> ProcessBoletoAttemptAsync(
        PaymentAttemptData attempt,
        bool recovery,
        CancellationToken cancellationToken)
    {
        var boletoContext = new BoletoConnectionContext(attempt.TenantId, attempt.ConnectionId, attempt.Provider);
        var persistedState = await repository.GetBoletoAttemptAsync(attempt.TenantId, attempt.Id, cancellationToken);
        if (persistedState is null)
            return CreatePublicPaymentAttemptFailureResult(ErrorCodes.Conflict, "Não foi possível recuperar este pagamento.");

        if (recovery || persistedState.ProviderPaymentId is not null)
            return await QueryBoletoAttemptAsync(attempt, boletoContext, persistedState, cancellationToken);

        PaymentChargeCreationContext? chargeContext;
        try
        {
            chargeContext = await repository.GetPaymentChargeCreationContextAsync(
                attempt.TenantId,
                attempt.ChargeId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is System.Data.Common.DbException or IOException or TimeoutException)
        {
            logger.LogWarning(
                "Boleto payer context could not be loaded. AttemptId {AttemptId}; ExceptionType {ExceptionType}",
                attempt.Id,
                exception.GetType().Name);
            return await PersistBoletoUnknownAsync(attempt, "BoletoPayerContextUnavailable", cancellationToken);
        }

        if (chargeContext?.Payer is null || chargeContext.DueDate == default)
            return await RejectBoletoAttemptAsync(attempt, "BoletoPayerUnavailable", null, cancellationToken);

        BoletoOperationResult<BoletoCharge> providerResult;
        try
        {
            providerResult = await boletos!.CreateAsync(new CreateBoletoChargeData(
                boletoContext,
                attempt.Id,
                attempt.ProviderExternalId,
                attempt.Amount,
                chargeContext.DueDate,
                new BoletoPayer(chargeContext.Payer.CustomerId, chargeContext.Payer.Name, chargeContext.Payer.TaxId)),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await PersistBoletoUnknownAsync(attempt, "ProviderOutcomeUnknown", CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogError(
                "Boleto provider call failed. AttemptId {AttemptId}; ConnectionId {ConnectionId}; ExceptionType {ExceptionType}",
                attempt.Id,
                attempt.ConnectionId,
                exception.GetType().Name);
            return await PersistBoletoUnknownAsync(attempt, "ProviderOutcomeUnknown", CancellationToken.None);
        }

        if (providerResult.Outcome is BoletoOperationOutcome.Rejected or BoletoOperationOutcome.ConfigurationError)
            return await RejectBoletoAttemptAsync(attempt, providerResult.ErrorCode ?? "ProviderRejected",
                providerResult.FailureMessage, CancellationToken.None);
        if (providerResult.Outcome != BoletoOperationOutcome.Completed || providerResult.Value is null)
            return await PersistBoletoUnknownAsync(attempt, providerResult.ErrorCode ?? "ProviderOutcomeUnknown",
                CancellationToken.None, providerResult.FailureMessage);
        if (!MatchesBoletoAttempt(attempt, providerResult.Value)
            || providerResult.Value.State == BoletoPaymentState.Pending
                && string.IsNullOrWhiteSpace(providerResult.Value.IdentificationField))
            return await PersistBoletoUnknownAsync(attempt, "ProviderChargeMismatch", CancellationToken.None);

        await repository.PersistBoletoResultAsync(attempt, providerResult.Value, CancellationToken.None);
        return BoletoResponse(attempt, providerResult.Value);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> QueryBoletoAttemptAsync(
        PaymentAttemptData attempt,
        BoletoConnectionContext context,
        BoletoAttemptState state,
        CancellationToken cancellationToken)
    {
        BoletoOperationResult<BoletoCharge> queried;
        try
        {
            queried = await boletos!.GetAsync(new BoletoChargeQuery(
                context,
                attempt.Id,
                attempt.ProviderExternalId,
                state.ProviderPaymentId), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await PersistBoletoUnknownAsync(attempt, "ProviderOutcomeUnknown", CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogError(
                "Boleto provider recovery failed. AttemptId {AttemptId}; ConnectionId {ConnectionId}; ExceptionType {ExceptionType}",
                attempt.Id,
                attempt.ConnectionId,
                exception.GetType().Name);
            return await PersistBoletoUnknownAsync(attempt, "ProviderOutcomeUnknown", CancellationToken.None);
        }

        if (queried.Outcome != BoletoOperationOutcome.Completed || queried.Value is null
            || !MatchesBoletoAttempt(attempt, queried.Value))
            return await PersistBoletoUnknownAsync(attempt, queried.ErrorCode ?? "BoletoReconciliationUnresolved",
                CancellationToken.None, queried.FailureMessage);

        await repository.PersistBoletoResultAsync(attempt, queried.Value, CancellationToken.None);
        return BoletoResponse(attempt, queried.Value);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> RejectBoletoAttemptAsync(
        PaymentAttemptData attempt,
        string failureCode,
        string? failureMessage,
        CancellationToken cancellationToken)
    {
        await repository.MarkAttemptRejectedAsync(attempt.Id, SafeFailureCode(failureCode) ?? "ProviderRejected",
            cancellationToken, failureMessage);
        return NotificationResult<PublicPaymentAttemptResult>.Success(new(
            "Failed", attempt.Amount, null, null, null, true,
            "Não foi possível gerar o boleto. Confira os dados da cobrança e tente novamente.")
        {
            InstrumentType = "Boleto"
        });
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> PersistBoletoUnknownAsync(
        PaymentAttemptData attempt,
        string failureCode,
        CancellationToken cancellationToken,
        string? failureMessage = null)
    {
        await repository.MarkAttemptUnknownAsync(attempt.Id, SafeFailureCode(failureCode) ?? "ProviderOutcomeUnknown",
            cancellationToken, failureMessage);
        return NotificationResult<PublicPaymentAttemptResult>.Success(new(
            "Unknown", attempt.Amount, null, null, null, false,
            "Não foi possível confirmar a emissão do boleto. Atualize a página para consultar a situação.")
        {
            InstrumentType = "Boleto"
        });
    }

    private static NotificationResult<PublicPaymentAttemptResult> BoletoResponse(
        PaymentAttemptData attempt,
        BoletoCharge charge)
    {
        var confirmed = charge.State is BoletoPaymentState.Confirmed or BoletoPaymentState.Received;
        var failed = charge.State is BoletoPaymentState.Overdue or BoletoPaymentState.Cancelled or BoletoPaymentState.Refunded;
        var status = confirmed ? "Succeeded" : failed ? "Failed" : charge.State == BoletoPaymentState.Pending
            ? "AwaitingPayment" : "Unknown";
        var message = confirmed ? "Pagamento confirmado. A baixa está sendo processada."
            : failed ? "Este boleto não está mais disponível. Gere uma nova tentativa para continuar."
            : charge.State == BoletoPaymentState.Pending ? "Boleto gerado. Aguardando pagamento."
            : "Estamos verificando a situação do boleto.";
        return NotificationResult<PublicPaymentAttemptResult>.Success(new(
            status,
            attempt.Amount,
            null,
            null,
            null,
            failed,
            message)
        {
            InstrumentType = "Boleto",
            Boleto = new PublicBoletoDetails(charge.IdentificationField, charge.Barcode,
                charge.BankSlipNumber, charge.BankSlipUrl, charge.InvoiceUrl)
        });
    }

    private static bool MatchesBoletoAttempt(PaymentAttemptData attempt, BoletoCharge charge) =>
        charge.Amount == attempt.Amount && charge.ProviderReference == attempt.ProviderExternalId;
}
