using System.Text.Json;
using System.Text.RegularExpressions;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Asaas.Payments;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.Boleto;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments.Boleto;

internal sealed class AsaasBoletoPaymentProvider(
    AsaasConfigurationReader configuration,
    AsaasCustomerOperations customers,
    AsaasPaymentOperations payments) : IBoletoPaymentProvider
{
    public string Provider => "Asaas";

    public async Task<BoletoProviderResult<BoletoCharge>> CreateAsync(
        CreateBoletoChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (!IsValidCreateData(data, connection))
            return Result(BoletoProviderOutcome.Rejected, "Validation");

        try
        {
            await configuration.ReadAsync(connection, cancellationToken);
            var found = await payments.FindChargesAsync(data.ProviderReference, AsaasBillingType.Boleto,
                0, 2, connection, cancellationToken);
            if (found.Outcome != AsaasApiOutcome.Completed || found.Value is null)
                return MapFailure<AsaasPaymentPage<AsaasPaymentCharge>, BoletoCharge>(found,
                    data.Payer.TaxId, data.Payer.Name, data.ProviderReference);
            if (found.Value.HasMore || found.Value.Items.Count > 1)
                return Result(BoletoProviderOutcome.Indeterminate, "AmbiguousProviderCharges");

            var payment = found.Value.Items.SingleOrDefault();
            if (payment is not null && !MatchesPayment(payment, data.ProviderReference, data.Amount))
                return Result(BoletoProviderOutcome.Indeterminate, "ProviderChargeMismatch");

            if (payment is null)
            {
                var payer = new PaymentChargePayer(data.Payer.TaxId, data.Payer.Name, data.Payer.CustomerId);
                var customer = await customers.ResolveCustomerAsync(payer, data.Context.TenantId,
                    connection, cancellationToken);
                if (customer.Outcome != AsaasApiOutcome.Completed || customer.Value is null)
                    return MapFailure<AsaasCustomer, BoletoCharge>(customer,
                        data.Payer.TaxId, data.Payer.Name, data.ProviderReference);

                var created = await payments.CreateChargeAsync(new AsaasCreatePaymentCharge(
                    customer.Value.Id,
                    data.Amount,
                    data.DueDate,
                    data.ProviderReference,
                    data.Description), AsaasBillingType.Boleto, connection, cancellationToken);
                if (created.Outcome != AsaasApiOutcome.Completed || created.Value is null)
                    return MapFailure<AsaasPaymentCharge, BoletoCharge>(created,
                        data.Payer.TaxId, data.Payer.Name, data.ProviderReference, customer.Value.Id);
                payment = created.Value;
            }

            if (!MatchesPayment(payment, data.ProviderReference, data.Amount))
                return Result(BoletoProviderOutcome.Indeterminate, "ProviderChargeMismatch");
            return await ReadBoletoAsync(payment, connection, cancellationToken,
                data.ProviderReference, data.Amount, data.Payer.TaxId, data.Payer.Name);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Result(BoletoProviderOutcome.ConfigurationError, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            return Result(BoletoProviderOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    public async Task<BoletoProviderResult<BoletoCharge>> GetAsync(
        BoletoChargeQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (!IsValidQuery(query, connection))
            return Result(BoletoProviderOutcome.Indeterminate, "Validation");

        try
        {
            AsaasPaymentCharge? payment;
            if (query.ProviderPaymentId is not null)
            {
                var retrieved = await payments.GetChargeAsync(query.ProviderPaymentId,
                    AsaasBillingType.Boleto, connection, cancellationToken);
                if (retrieved.Outcome != AsaasApiOutcome.Completed || retrieved.Value is null)
                    return MapFailure<AsaasPaymentCharge, BoletoCharge>(retrieved, query.ProviderReference);
                payment = retrieved.Value;
            }
            else
            {
                var found = await payments.FindChargesAsync(query.ProviderReference,
                    AsaasBillingType.Boleto, 0, 2, connection, cancellationToken);
                if (found.Outcome != AsaasApiOutcome.Completed || found.Value is null)
                    return MapFailure<AsaasPaymentPage<AsaasPaymentCharge>, BoletoCharge>(found, query.ProviderReference);
                if (found.Value.HasMore || found.Value.Items.Count > 1)
                    return Result(BoletoProviderOutcome.Indeterminate, "AmbiguousProviderCharges");
                payment = found.Value.Items.SingleOrDefault();
                if (payment is null)
                    return Result(BoletoProviderOutcome.NotFound, null);
            }

            if (!MatchesPayment(payment, query.ProviderReference, expectedAmount: null))
                return Result(BoletoProviderOutcome.Indeterminate, "ProviderChargeMismatch");
            return await ReadBoletoAsync(payment, connection, cancellationToken, query.ProviderReference,
                expectedAmount: null);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Result(BoletoProviderOutcome.ConfigurationError, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            return Result(BoletoProviderOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    private async Task<BoletoProviderResult<BoletoCharge>> ReadBoletoAsync(
        AsaasPaymentCharge payment,
        ProviderConnection connection,
        CancellationToken cancellationToken,
        string expectedReference,
        decimal? expectedAmount,
        params string?[] sensitiveValues)
    {
        if (!MatchesPayment(payment, expectedReference, expectedAmount))
            return Result(BoletoProviderOutcome.Indeterminate, "ProviderChargeMismatch");

        var state = MapState(payment.Status, payment.Deleted);
        AsaasBoletoBillingInfo? billingInfo = null;
        if (state == BoletoPaymentState.Pending)
        {
            var billingInfoResult = await payments.GetBoletoBillingInfoAsync(payment.Id, connection, cancellationToken);
            if (billingInfoResult.Outcome != AsaasApiOutcome.Completed || billingInfoResult.Value is null)
                return MapFailure<AsaasBoletoBillingInfo, BoletoCharge>(billingInfoResult, sensitiveValues);
            billingInfo = billingInfoResult.Value;
        }

        var charge = new BoletoCharge(
            payment.Id,
            payment.Amount,
            expectedReference,
            state,
            payment.Status,
            billingInfo?.IdentificationField,
            billingInfo?.Barcode,
            billingInfo?.BankSlipNumber,
            billingInfo?.BankSlipUrl,
            payment.InvoiceUrl,
            billingInfo is null ? payment.ProviderPayloadJson : CombinePayload(payment.ProviderPayloadJson, billingInfo.ProviderPayloadJson))
        {
            Deleted = payment.Deleted
        };
        return new(BoletoProviderOutcome.Completed, charge);
    }

    private static BoletoProviderResult<TOutput> MapFailure<TInput, TOutput>(
        AsaasApiResult<TInput> result,
        params string?[] sensitiveValues)
    {
        var outcome = result.Outcome switch
        {
            AsaasApiOutcome.Rejected => BoletoProviderOutcome.Rejected,
            AsaasApiOutcome.ConfigurationError => BoletoProviderOutcome.ConfigurationError,
            _ => BoletoProviderOutcome.Indeterminate
        };
        var code = result.Diagnostic?.Errors.FirstOrDefault()?.Code;
        if (!SafeCode(code))
            code = result.ErrorCode;
        var descriptions = result.Diagnostic?.Errors.Select(error =>
        {
            var description = error.Description;
            foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                description = description?.Replace(sensitiveValue!, "[REDACTED]", StringComparison.OrdinalIgnoreCase);
                var digits = new string(sensitiveValue!.Where(char.IsAsciiDigit).ToArray());
                if (digits.Length is 11 or 14)
                    description = Regex.Replace(description ?? string.Empty,
                        string.Join(@"[\s./-]*", digits.Select(digit => digit.ToString())),
                        "[REDACTED]", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
            }
            return string.IsNullOrWhiteSpace(description) ? error.Code : $"{error.Code}: {description}";
        }).ToArray() ?? [];
        var status = result.Diagnostic?.HttpStatusCode is { } httpStatus
            ? $"HTTP {httpStatus}" : "resposta sem status HTTP";
        var message = result.Diagnostic is null ? null
            : $"Asaas {status}: {(descriptions.Length == 0 ? "sem detalhes adicionais." : string.Join(" | ", descriptions))}";
        if (message?.Length > 1000)
            message = message[..1000];
        return new BoletoProviderResult<TOutput>(outcome, default,
            SafeCode(code) ? code : outcome == BoletoProviderOutcome.Rejected ? "ProviderRejected" : "ProviderOutcomeUnknown")
        {
            FailureMessage = message
        };
    }

    private static bool MatchesPayment(AsaasPaymentCharge payment, string reference, decimal? expectedAmount) =>
        payment.Reference == reference && (expectedAmount is null || payment.Amount == expectedAmount)
        && payment.BillingType == AsaasBillingType.Boleto.ToApiValue();

    private static string CombinePayload(string paymentPayload, string billingInfoPayload) =>
        "{\"payment\":" + paymentPayload + ",\"billingInfo\":" + billingInfoPayload + "}";

    private static BoletoPaymentState MapState(string status, bool deleted) => deleted || status == "CANCELED"
        ? BoletoPaymentState.Cancelled
        : status switch
        {
            "PENDING" => BoletoPaymentState.Pending,
            "CONFIRMED" => BoletoPaymentState.Confirmed,
            "RECEIVED" => BoletoPaymentState.Received,
            "OVERDUE" => BoletoPaymentState.Overdue,
            "REFUNDED" or "REFUND_REQUESTED" or "REFUND_IN_PROGRESS" => BoletoPaymentState.Refunded,
            _ => BoletoPaymentState.Unknown
        };

    private static BoletoProviderResult<BoletoCharge> Result(BoletoProviderOutcome outcome, string? errorCode) =>
        new(outcome, null, errorCode);

    private static bool IsValidCreateData(CreateBoletoChargeData data, ProviderConnection connection) =>
        data.Context.TenantId != Guid.Empty && data.Context.ConnectionId == connection.Id
        && connection.TenantId == data.Context.TenantId && connection.Enabled
        && connection.Provider == "Asaas" && connection.Group == "PaymentProvider"
        && data.OperationId != Guid.Empty && Reference(data.ProviderReference)
        && data.Amount > 0 && decimal.Round(data.Amount, 2) == data.Amount && data.DueDate != default
        && !string.IsNullOrWhiteSpace(data.Payer.Name) && ValidTaxId(data.Payer.TaxId);

    private static bool IsValidQuery(BoletoChargeQuery query, ProviderConnection connection) =>
        query.Context.TenantId != Guid.Empty && query.Context.ConnectionId == connection.Id
        && connection.TenantId == query.Context.TenantId && connection.Provider == "Asaas"
        && connection.Group == "PaymentProvider" && query.Context.ExpectedProvider == "Asaas"
        && Reference(query.ProviderReference);

    private static bool ValidTaxId(string value) => value.Length is 11 or 14 && value.All(char.IsAsciiDigit);
    private static bool Reference(string? value) => value is { Length: > 0 and <= 200 } && !value.Any(char.IsControl);
    private static bool SafeCode(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
    private static bool IsProviderFailure(Exception exception) => exception is HttpRequestException or IOException
        or TimeoutException or JsonException or InvalidOperationException;
}
