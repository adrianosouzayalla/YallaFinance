using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;

namespace YallaFinance.Infrastructure.Services.Payments;

// The public attempt service owns authorization/reservation and orchestrates explicit card operations.
public sealed partial class PostgresPublicPaymentAttemptService
{
    private async Task<NotificationResult<PublicPaymentAttemptResult>> ProcessCardAttemptAsync(PaymentAttemptData attempt,
        CardCheckoutContext? checkout, CreditCardPaymentInput? payment, bool recovery, bool sameHolder, CancellationToken ct)
    {
        var context = new CreditCardConnectionContext(attempt.TenantId, attempt.ConnectionId, attempt.Provider);
        var state = await repository.GetCardAttemptAsync(attempt.TenantId, attempt.Id, ct);
        if (state is null) return CreatePublicPaymentAttemptFailureResult(ErrorCodes.Conflict, "Não foi possível recuperar este pagamento.");
        var operation = "validar os dados da tentativa";
        try
        {
            if (recovery || state.PaymentDispatchedAt is not null)
                return await QueryCardAttemptAsync(attempt, context, state, ct);
            if (checkout is null || payment is null || checkout.TenantId != attempt.TenantId || checkout.ChargeId != attempt.ChargeId)
                return await CardUnknownAsync(attempt, "CardInputUnavailable",
                    "Não foi possível carregar os dados necessários para processar o cartão.");

            var customerId = state.CustomerId;
            if (customerId is null)
            {
                var customer = checkout.Customer;
                var customerDocument = sameHolder ? payment.Document : Digits(customer.Document);
                var customerName = sameHolder ? payment.PayerName : customer.Name;
                var reference = $"yalla:{attempt.TenantId:N}:customer:{customer.Id:N}";
                operation = "consultar o cliente no provider";
                var found = await cards!.FindCustomersAsync(new(context, reference, 0, 2), ct);
                if (found.Outcome != CreditCardOperationOutcome.Completed || found.Value is null
                    || found.Value.HasMore || found.Value.Items.Count > 1)
                    return await CardUnknownAsync(attempt, found.ErrorCode ?? "CustomerLookupUnresolved",
                        diagnostic: found.Diagnostic);
                var remoteCustomer = found.Value.Items.SingleOrDefault();
                if (remoteCustomer is not null && remoteCustomer.Document != customerDocument)
                    return await CardUnknownAsync(attempt, "CustomerIdentityMismatch",
                        "O cliente retornado pelo provider não corresponde ao cadastro informado.");
                if (remoteCustomer is null)
                {
                    // Customer registration only needs identity. Do not send optional stale contact data,
                    // nor copy another cardholder's contact/document onto the charge's customer.
                    operation = "criar o cliente no provider";
                    var created = await cards.CreateCustomerAsync(new(context, attempt.Id, reference, customerName,
                        customerDocument), ct);
                    if (created.Outcome == CreditCardOperationOutcome.Rejected || created.Outcome == CreditCardOperationOutcome.ConfigurationError)
                    {
                        var failureMessage = FormatCardProviderFailure(attempt.Provider, created.ErrorCode ?? "CustomerRejected",
                            created.Diagnostic);
                        await repository.MarkAttemptRejectedAsync(attempt.Id, created.ErrorCode ?? "CustomerRejected",
                            CancellationToken.None, failureMessage);
                        return CardResponse(attempt, "Failed", true, failureMessage);
                    }
                    if (created.Outcome != CreditCardOperationOutcome.Completed || created.Value is null)
                        return await CardUnknownAsync(attempt, created.ErrorCode ?? "CustomerCreationUnresolved",
                            diagnostic: created.Diagnostic);
                    remoteCustomer = created.Value;
                }
                customerId = remoteCustomer.Id;
                await repository.SaveCardIdentityAsync(attempt, customerId, null, null, CancellationToken.None);
            }

            CreditCardCharge? charge = null;
            if (state.ChargeId is not null)
            {
                operation = "consultar a cobrança no provider";
                var existing = await cards!.GetChargeAsync(new(context, state.ChargeId), ct);
                if (existing.Outcome != CreditCardOperationOutcome.Completed || existing.Value is null)
                    return await CardUnknownAsync(attempt, existing.ErrorCode ?? "ChargeLookupUnresolved",
                        diagnostic: existing.Diagnostic);
                charge = existing.Value;
            }
            else
            {
                operation = "localizar a cobrança pela referência da tentativa";
                var found = await cards!.FindChargesAsync(new(context, attempt.ProviderExternalId, 0, 2), ct);
                if (found.Outcome != CreditCardOperationOutcome.Completed || found.Value is null)
                    return await CardUnknownAsync(attempt, found.ErrorCode ?? "ChargeLookupUnresolved",
                        diagnostic: found.Diagnostic);
                if (found.Value.HasMore || found.Value.Items.Count > 1)
                    return await CardUnknownAsync(attempt, found.ErrorCode ?? "ChargeLookupUnresolved",
                        CreateCardChargeLookupMessage(attempt.Provider, found.Value), found.Diagnostic);
                charge = found.Value.Items.SingleOrDefault();
                if (charge is null)
                {
                    operation = "criar a cobrança no provider";
                    var created = await cards.CreateChargeAsync(new(context, attempt.Id, customerId, attempt.Amount,
                        checkout.DueDate, attempt.ProviderExternalId), ct);
                    if (created.Outcome is CreditCardOperationOutcome.Rejected or CreditCardOperationOutcome.ConfigurationError)
                    {
                        var failureMessage = FormatCardProviderFailure(attempt.Provider, created.ErrorCode ?? "ChargeRejected",
                            created.Diagnostic);
                        await repository.MarkAttemptRejectedAsync(attempt.Id, created.ErrorCode ?? "ChargeRejected",
                            CancellationToken.None, failureMessage);
                        return CardResponse(attempt, "Failed", true, failureMessage);
                    }
                    if (created.Outcome != CreditCardOperationOutcome.Completed || created.Value is null)
                        return await CardUnknownAsync(attempt, created.ErrorCode ?? "ChargeCreationUnresolved",
                            diagnostic: created.Diagnostic);
                    charge = created.Value;
                }
            }
            if (!MatchesCardAttempt(attempt, charge, customerId))
                return await CardUnknownAsync(attempt, "ChargeIdentityMismatch",
                    "A cobrança retornada pelo provider não corresponde a esta tentativa.");
            await repository.SaveCardIdentityAsync(attempt, customerId, charge.Id, charge.ProviderStatus, CancellationToken.None);
            if (charge.Deleted || charge.State != CreditCardPaymentState.Pending)
                return await PersistCardResponseAsync(attempt, charge);
            if (!await repository.IsPaymentRequestActiveAsync(attempt.TenantId, attempt.PaymentRequestId, ct))
                return await CardUnknownAsync(attempt, "PaymentRequestNoLongerActive",
                    "A solicitação de pagamento não está mais ativa.");
            ct.ThrowIfCancellationRequested();
            if (!await repository.TryMarkCardDispatchedAsync(attempt, CancellationToken.None))
                return await QueryCardAttemptAsync(attempt, context, new(customerId, charge.Id, charge.ProviderStatus, clock.GetUtcNow().UtcDateTime), ct);

            // After the durable mark, even a disconnect cannot cause a second dispatch.
            operation = "enviar o pagamento com cartão ao provider";
            var paid = await cards.PayChargeAsync(new(context, attempt.Id, charge.Id, customerId, attempt.Amount,
                attempt.ProviderExternalId, payment), CancellationToken.None);
            if (paid.Outcome == CreditCardOperationOutcome.Rejected)
                return await HandleCardRejectionAsync(attempt, context, charge, paid.ErrorCode, paid.Diagnostic, ct);
            if (paid.Outcome != CreditCardOperationOutcome.Completed || paid.Value is null)
                return await CardUnknownAsync(attempt, paid.ErrorCode ?? "PaymentOutcomeUnknown",
                    diagnostic: paid.Diagnostic);
            if (!MatchesCardAttempt(attempt, paid.Value, customerId) || paid.Value.Id != charge.Id)
                return await CardUnknownAsync(attempt, "PaymentIdentityMismatch",
                    "O retorno do provider não corresponde à cobrança desta tentativa.");
            return await PersistCardResponseAsync(attempt, paid.Value);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Never log the exception or inputs: provider exceptions can contain card data.
            return await CardUnknownAsync(attempt, "CardProcessingUnresolved",
                $"{attempt.Provider}: falha durante {operation} ({exception.GetType().Name}); o resultado do pagamento não foi confirmado.");
        }
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> QueryCardAttemptAsync(PaymentAttemptData attempt,
        CreditCardConnectionContext context, CardAttemptState state, CancellationToken ct)
    {
        CreditCardResult<CreditCardCharge> queried;
        if (state.ChargeId is not null)
        {
            queried = await cards!.GetChargeAsync(new(context, state.ChargeId), ct);
            if (queried.Outcome != CreditCardOperationOutcome.Completed || queried.Value is null)
                return await CardUnknownAsync(attempt, queried.ErrorCode ?? "ChargeLookupUnresolved",
                    diagnostic: queried.Diagnostic);
        }
        else
        {
            var found = await cards!.FindChargesAsync(new(context, attempt.ProviderExternalId, 0, 2), ct);
            if (found.Outcome != CreditCardOperationOutcome.Completed || found.Value is null)
                return await CardUnknownAsync(attempt, found.ErrorCode ?? "ChargeNotReconciled",
                    diagnostic: found.Diagnostic);
            if (found.Value.HasMore || found.Value.Items.Count != 1)
                return await CardUnknownAsync(attempt, found.ErrorCode ?? "ChargeNotReconciled",
                    CreateCardChargeLookupMessage(attempt.Provider, found.Value), found.Diagnostic);
            queried = new(CreditCardOperationOutcome.Completed, found.Value.Items[0]);
        }
        if (queried.Outcome != CreditCardOperationOutcome.Completed || queried.Value is null
            || state.CustomerId is null || !MatchesCardAttempt(attempt, queried.Value, state.CustomerId))
            return await CardUnknownAsync(attempt, queried.ErrorCode ?? "CardReconciliationUnresolved",
                "A consulta do provider não confirmou uma cobrança compatível com esta tentativa.", queried.Diagnostic);
        await repository.SaveCardIdentityAsync(attempt, state.CustomerId, queried.Value.Id, queried.Value.ProviderStatus, CancellationToken.None);
        if (state.PaymentDispatchedAt is null && queried.Value.State == CreditCardPaymentState.Pending && !queried.Value.Deleted)
            return await CardUnknownAsync(attempt, "CardPaymentNotDispatched",
                $"{attempt.Provider}: a cobrança foi localizada, mas o envio do pagamento com cartão não foi iniciado. Tente novamente.");
        // Recovery never dispatches payment; a pending charge without a dispatch marker is made retryable above.
        return await PersistCardResponseAsync(attempt, queried.Value);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> PersistCardResponseAsync(PaymentAttemptData attempt, CreditCardCharge charge)
    {
        await repository.PersistCardResultAsync(attempt, charge, CancellationToken.None);
        return charge.State is CreditCardPaymentState.Confirmed or CreditCardPaymentState.Received
            ? CardResponse(attempt, "Succeeded", false, "Pagamento confirmado. A baixa está sendo processada.")
            : charge.Deleted ? CardResponse(attempt, "Failed", true, "O cartão não foi aprovado. Confira os dados ou use outro cartão.")
            : CardResponse(attempt, "AwaitingPayment", false, "Aguardando confirmação do pagamento com cartão.");
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> HandleCardRejectionAsync(PaymentAttemptData attempt,
        CreditCardConnectionContext context,
        CreditCardCharge charge,
        string? errorCode,
        CreditCardProviderDiagnostic? diagnostic,
        CancellationToken ct)
    {
        var queried = await cards!.GetChargeAsync(new(context, charge.Id), CancellationToken.None);
        if (queried.Outcome != CreditCardOperationOutcome.Completed || queried.Value is null
            || !MatchesCardAttempt(attempt, queried.Value, charge.CustomerId) || queried.Value.Id != charge.Id)
            return await CardUnknownAsync(attempt, queried.ErrorCode ?? errorCode ?? "RejectedCardNotReconciled",
                diagnostic: queried.Diagnostic ?? diagnostic);
        if (queried.Value.State is CreditCardPaymentState.Confirmed or CreditCardPaymentState.Received)
            return await PersistCardResponseAsync(attempt, queried.Value);
        if (queried.Value.Deleted) return await PersistCardResponseAsync(attempt, queried.Value);
        if (queried.Value.State != CreditCardPaymentState.Pending)
            return await PersistCardResponseAsync(attempt, queried.Value);
        var cancelled = await cards.CancelChargeAsync(new(context, charge.Id), CancellationToken.None);
        if (cancelled.Outcome != CreditCardOperationOutcome.Completed || cancelled.Value is not { Deleted: true }
            || cancelled.Value.Id != charge.Id)
            return await CardUnknownAsync(attempt, cancelled.ErrorCode ?? "CardCancellationUnconfirmed",
                diagnostic: cancelled.Diagnostic ?? diagnostic);
        // A successful delete does not mean refund: verify state again and prefer any confirmed payment.
        var after = await cards.GetChargeAsync(new(context, charge.Id), CancellationToken.None);
        if (after.Outcome != CreditCardOperationOutcome.Completed || after.Value is null
            || after.Value.Id != charge.Id || !MatchesCardAttempt(attempt, after.Value, charge.CustomerId))
            return await CardUnknownAsync(attempt, after.ErrorCode ?? "CardCancellationNotReconciled",
                diagnostic: after.Diagnostic ?? diagnostic);
        if (after.Value.State is CreditCardPaymentState.Confirmed or CreditCardPaymentState.Received || after.Value.Deleted)
            return await PersistCardResponseAsync(attempt, after.Value);
        return await CardUnknownAsync(attempt, "CardCancellationNotReconciled",
            "O provider não confirmou o encerramento da cobrança recusada.", diagnostic);
    }

    private async Task<NotificationResult<PublicPaymentAttemptResult>> CardUnknownAsync(PaymentAttemptData attempt,
        string code,
        string? message = null,
        CreditCardProviderDiagnostic? diagnostic = null)
    {
        var failureCode = SafeFailureCode(code) ?? "CardOutcomeUnknown";
        var failureMessage = message ?? FormatCardProviderFailure(attempt.Provider, failureCode, diagnostic);
        var recordedFailure = await repository.RecordCardAttemptFailureAsync(attempt, failureCode, failureMessage,
            CancellationToken.None);
        var resultMessage = recordedFailure.Status switch
        {
            "Succeeded" => "Pagamento confirmado. A baixa está sendo processada.",
            "Canceled" or "Expired" => "Esta tentativa de pagamento foi encerrada.",
            _ => recordedFailure.Message ?? failureMessage
        };
        if (recordedFailure.Status == "Unknown" && message is null && diagnostic is null)
            resultMessage = "Estamos verificando o resultado do pagamento com cartão. Não é necessário pagar novamente.";
        return CardResponse(attempt, recordedFailure.Status, recordedFailure.CanRetry, resultMessage);
    }
    private static string FormatCardProviderFailure(string provider, string failureCode,
        CreditCardProviderDiagnostic? diagnostic)
    {
        var errors = diagnostic?.Errors.Select(error => string.IsNullOrWhiteSpace(error.Description)
            ? error.Code
            : $"{error.Code}: {error.Description}").Where(message => !string.IsNullOrWhiteSpace(message)).ToArray() ?? [];
        var providerResponse = diagnostic?.HttpStatusCode is { } statusCode ? $"{provider} HTTP {statusCode}" : provider;
        return errors.Length > 0
            ? $"{providerResponse}: {string.Join("; ", errors)}"
            : diagnostic?.HttpStatusCode is not null
                ? $"{providerResponse}: {failureCode}"
                : $"{provider}: não houve confirmação do resultado da operação (código {failureCode}).";
    }

    private static string CreateCardChargeLookupMessage(string provider, CreditCardPage<CreditCardCharge>? result) => result is null
        ? $"{provider}: a consulta da cobrança não retornou um resultado válido."
        : result.Items.Count == 0 && !result.HasMore
            ? $"{provider}: consulta concluída, mas nenhuma cobrança foi encontrada para a referência desta tentativa."
            : $"{provider}: a consulta não identificou uma única cobrança para a referência desta tentativa.";
    private static bool MatchesCardAttempt(PaymentAttemptData attempt, CreditCardCharge charge, string customerId) =>
        charge.CustomerId == customerId && charge.Amount == attempt.Amount && charge.Reference == attempt.ProviderExternalId;
    private static string Digits(string value) => new(value.Where(char.IsAsciiDigit).ToArray());
    private static NotificationResult<PublicPaymentAttemptResult> CardResponse(PaymentAttemptData attempt, string status,
        bool retry, string message) => NotificationResult<PublicPaymentAttemptResult>.Success(new(status, attempt.Amount,
            null, null, null, retry, message) { InstrumentType = "CreditCard" });
}
