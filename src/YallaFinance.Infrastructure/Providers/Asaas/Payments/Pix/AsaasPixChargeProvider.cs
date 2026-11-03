using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Asaas.Payments;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.Pix;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments.Pix;

internal sealed class AsaasPixChargeProvider(
    AsaasConfigurationReader configuration,
    AsaasCustomerOperations customers,
    AsaasPaymentOperations payments,
    AsaasPixOperations pixOperations,
    TimeProvider clock,
    ILogger<AsaasPixChargeProvider> logger) : IPixChargeProvider
{
    private static readonly TimeSpan MaximumRequestTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RecoveryGracePeriod = TimeSpan.FromMinutes(5);

    public string Provider => "Asaas";

    public async Task<CreatePixChargeResult> CreateAsync(
        CreatePixChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        CreatePixChargeResult Result(
            PixChargeCreationOutcome outcome,
            string? error,
            PixChargeSnapshot? charge = null,
            bool alreadyExisted = false,
            string? failureMessage = null) =>
            new CreatePixChargeResult(outcome, connection.Id, Provider, data.ProviderReference, charge, error, alreadyExisted)
            {
                FailureMessage = failureMessage
            };

        if (!IsValidCreateData(data, connection))
            return Result(PixChargeCreationOutcome.Rejected, "Validation");
        if (data.Payer is null)
            return Result(PixChargeCreationOutcome.Rejected, "PixPayerMissing");
        if (data.DueDate is not { } dueDate || dueDate == default)
            return Result(PixChargeCreationOutcome.Rejected, "PixDueDateMissing");

        try
        {
            await configuration.ReadAsync(connection, cancellationToken);
            var existing = await payments.FindChargesAsync(
                data.ProviderReference,
                AsaasBillingType.Pix,
                0,
                2,
                connection,
                cancellationToken);
            if (existing.Outcome != AsaasApiOutcome.Completed || existing.Value is null)
            {
                var failure = GetProviderFailure(existing, data.ProviderReference, data.Description);
                return Result(ToCreationOutcome(existing.Outcome), failure.ErrorCode, failureMessage: failure.FailureMessage);
            }
            if (existing.Value.HasMore || existing.Value.Items.Count > 1)
                return Result(PixChargeCreationOutcome.Rejected, "AmbiguousProviderCharges");

            var alreadyExisted = existing.Value.Items.Count == 1;
            AsaasPaymentCharge payment;
            if (alreadyExisted)
            {
                payment = existing.Value.Items[0];
                if (!IsMatchingPayment(payment, data))
                    return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeMismatch");
            }
            else
            {
                var customer = await customers.ResolvePixCustomerAsync(
                    data.Payer!,
                    data.TenantId,
                    connection,
                    cancellationToken);
                if (customer.Outcome != AsaasApiOutcome.Completed || customer.Value is null)
                {
                    var failure = GetProviderFailure(customer, data.Payer.Name, data.Payer.TaxId);
                    return Result(ToCreationOutcome(customer.Outcome), failure.ErrorCode,
                        failureMessage: failure.FailureMessage);
                }

                var createPayment = await payments.CreateChargeAsync(
                    new AsaasCreatePaymentCharge(
                        customer.Value.Id,
                        data.Amount,
                        data.DueDate!.Value,
                        data.ProviderReference,
                        data.Description),
                    AsaasBillingType.Pix,
                    connection,
                    cancellationToken);
                if (createPayment.Outcome != AsaasApiOutcome.Completed || createPayment.Value is null)
                {
                    var failure = GetProviderFailure(createPayment, data.Payer.Name, data.Payer.TaxId,
                        data.ProviderReference, data.Description, customer.Value.Id);
                    return Result(ToCreationOutcome(createPayment.Outcome), failure.ErrorCode,
                        failureMessage: failure.FailureMessage);
                }
                payment = createPayment.Value;
                if (payment.CustomerId != customer.Value.Id || !IsMatchingPayment(payment, data))
                    return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeMismatch");
            }

            return await CreateSnapshotAsync(payment, data, connection, cancellationToken, alreadyExisted);
        }
        catch (IntegrationConfigurationException exception)
        {
            return Result(PixChargeCreationOutcome.Rejected, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            logger.LogError("Asaas Pix payment creation failed. ConnectionId {ConnectionId}; AttemptId {AttemptId}; ExceptionType {ExceptionType}",
                connection.Id, data.OperationId, exception.GetType().Name);
            return Result(PixChargeCreationOutcome.Rejected, "ProviderOutcomeUnknown");
        }
    }

    public async Task<PixChargeQueryResult> GetAsync(
        PixChargeQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        PixChargeQueryResult Result(PixChargeQueryOutcome outcome, string? error, PixChargeSnapshot? charge = null,
            string? failureMessage = null) =>
            new PixChargeQueryResult(outcome, connection.Id, Provider, query.ProviderReference, charge, error)
            {
                FailureMessage = failureMessage
            };
        if (!IsValidQuery(query, connection))
            return Result(PixChargeQueryOutcome.Indeterminate, "Validation");

        try
        {
            if (query.AttemptCreatedAt is not { } createdAt)
                return Result(PixChargeQueryOutcome.Indeterminate, "AttemptCreationTimeUnavailable");

            var found = await payments.FindChargesAsync(
                query.ProviderReference,
                AsaasBillingType.Pix,
                0,
                2,
                connection,
                cancellationToken);
            if (found.Outcome != AsaasApiOutcome.Completed || found.Value is null)
            {
                var failure = GetProviderFailure(found, query.ProviderReference);
                return Result(PixChargeQueryOutcome.Indeterminate, failure.ErrorCode ?? "ProviderOutcomeUnknown",
                    failureMessage: failure.FailureMessage);
            }
            if (found.Value.HasMore || found.Value.Items.Count > 1)
                return Result(PixChargeQueryOutcome.Indeterminate, "AmbiguousProviderCharges");
            if (found.Value.Items.Count == 0)
            {
                var recoveryAt = createdAt.Add(MaximumRequestTimeout).Add(RecoveryGracePeriod);
                return clock.GetUtcNow() < recoveryAt
                    ? Result(PixChargeQueryOutcome.Indeterminate, "PaymentRecoveryPending")
                    : Result(PixChargeQueryOutcome.NotFound, null);
            }

            var payment = found.Value.Items[0];
            if (payment.Deleted || payment.Status == "OVERDUE")
                return Result(PixChargeQueryOutcome.NotFound, null);

            var snapshotResult = await CreateSnapshotAsync(
                payment,
                new CreatePixChargeData(
                    query.TenantId,
                    query.ConnectionId,
                    query.ExpectedProvider,
                    Guid.Empty,
                    query.ProviderReference,
                    payment.Amount,
                    0),
                connection,
                cancellationToken,
                alreadyExisted: true);
            return snapshotResult.Outcome switch
            {
                PixChargeCreationOutcome.Completed when snapshotResult.Charge is not null =>
                    Result(PixChargeQueryOutcome.Found, null, snapshotResult.Charge),
                PixChargeCreationOutcome.Rejected when snapshotResult.ErrorCode == "PixExpired" =>
                    Result(PixChargeQueryOutcome.NotFound, null),
                _ => Result(PixChargeQueryOutcome.Indeterminate,
                    snapshotResult.ErrorCode ?? "ProviderOutcomeUnknown",
                    failureMessage: snapshotResult.FailureMessage)
            };
        }
        catch (IntegrationConfigurationException exception)
        {
            return Result(PixChargeQueryOutcome.Indeterminate, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsProviderFailure(exception))
        {
            logger.LogWarning("Asaas Pix payment recovery failed. ConnectionId {ConnectionId}; ExceptionType {ExceptionType}",
                connection.Id, exception.GetType().Name);
            return Result(PixChargeQueryOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    private async Task<CreatePixChargeResult> CreateSnapshotAsync(
        AsaasPaymentCharge payment,
        CreatePixChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken,
        bool alreadyExisted)
    {
        CreatePixChargeResult Result(PixChargeCreationOutcome outcome, string? error, PixChargeSnapshot? charge = null,
            string? failureMessage = null) =>
            new CreatePixChargeResult(outcome, connection.Id, Provider, data.ProviderReference, charge, error, alreadyExisted)
            {
                FailureMessage = failureMessage
            };

        if (payment.Deleted || payment.Status == "OVERDUE")
            return Result(PixChargeCreationOutcome.Rejected, "PixExpired");
        if (!IsMatchingPayment(payment, data))
            return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeMismatch");
        if (payment.Status != "PENDING")
            return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeNotPending");

        var observedAt = clock.GetUtcNow();
        var result = await pixOperations.GetPaymentQrCodeAsync(payment.Id, connection, cancellationToken);
        if (result.Outcome != AsaasApiOutcome.Completed || result.Value is null)
        {
            var failure = GetProviderFailure(result, payment.Id, data.Payer?.Name, data.Payer?.TaxId,
                data.ProviderReference, data.Description);
            logger.LogWarning(
                "Asaas Pix QR code retrieval failed. ConnectionId {ConnectionId}; PaymentId {PaymentId}; Outcome {Outcome}; ErrorCode {ErrorCode}; HttpStatusCode {HttpStatusCode}; ProviderErrors {ProviderErrors}",
                connection.Id,
                payment.Id,
                result.Outcome,
                failure.ErrorCode ?? "ProviderOutcomeUnknown",
                result.Diagnostic?.HttpStatusCode,
                failure.FailureMessage ?? "None");
            return Result(PixChargeCreationOutcome.Rejected,
                failure.ErrorCode ?? "ProviderOutcomeUnknown", failureMessage: failure.FailureMessage);
        }
        if (result.Value.ExpiresAt <= observedAt)
            return Result(PixChargeCreationOutcome.Rejected, "PixExpired");

        var snapshot = new PixChargeSnapshot(
            data.ProviderReference,
            data.Amount,
            observedAt,
            result.Value.ExpiresAt,
            PixChargeStatus.Active,
            result.Value.Payload,
            clock.GetUtcNow(),
            null,
            data.Description);
        return snapshot.IsPayable
            ? Result(PixChargeCreationOutcome.Completed, null, snapshot)
            : Result(PixChargeCreationOutcome.Rejected, "InstrumentUnavailable", snapshot);
    }

    private static bool IsMatchingPayment(AsaasPaymentCharge payment, CreatePixChargeData data) =>
        payment.Amount == data.Amount
        && string.Equals(payment.Reference, data.ProviderReference, StringComparison.Ordinal)
        && string.Equals(payment.BillingType, AsaasBillingType.Pix.ToApiValue(), StringComparison.Ordinal);

    private static (string? ErrorCode, string? FailureMessage) GetProviderFailure<T>(
        AsaasApiResult<T> result,
        params string?[] sensitiveValues)
    {
        var providerErrorCode = result.Diagnostic?.Errors.FirstOrDefault()?.Code;
        var errorCode = IsSafeFailureCode(providerErrorCode) ? providerErrorCode : result.ErrorCode;
        if (result.Diagnostic is not { } diagnostic)
            return (errorCode, null);

        var providerErrors = diagnostic.Errors.Select(error =>
        {
            var code = IsSafeFailureCode(error.Code) ? error.Code : "ProviderError";
            var description = RedactSensitiveValues(error.Description, sensitiveValues);
            return string.IsNullOrWhiteSpace(description) ? code : $"{code}: {description}";
        }).ToArray();
        var status = diagnostic.HttpStatusCode is { } httpStatusCode
            ? $"HTTP {httpStatusCode}"
            : "resposta sem status HTTP";
        var message = providerErrors.Length > 0
            ? $"Asaas {status}: {string.Join(" | ", providerErrors)}"
            : $"Asaas {status}: sem detalhes adicionais.";
        message = new string(message.Where(character => !char.IsControl(character)).ToArray());
        if (message.Length > 1000)
            message = message[..1000];
        return (errorCode, message);
    }

    private static string? RedactSensitiveValues(string? value, IReadOnlyList<string?> sensitiveValues)
    {
        if (value is null)
            return null;

        foreach (var sensitiveValue in sensitiveValues.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            value = value.Replace(sensitiveValue!, "[redacted]", StringComparison.OrdinalIgnoreCase);
            var digits = new string(sensitiveValue!.Where(char.IsAsciiDigit).ToArray());
            if (digits.Length is 11 or 14)
            {
                var pattern = string.Join(@"[\s./-]*", digits.Select(digit => Regex.Escape(digit.ToString())));
                value = Regex.Replace(value, pattern, "[redacted]", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
            }
        }

        return value;
    }

    private static bool IsSafeFailureCode(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private static bool IsValidCreateData(CreatePixChargeData data, ProviderConnection connection) =>
        data.TenantId != Guid.Empty && data.ConnectionId == connection.Id && connection.TenantId == data.TenantId
        && connection.Enabled && connection.Provider == "Asaas" && connection.Group == "PaymentProvider"
        && data.OperationId != Guid.Empty && Reference(data.ProviderReference)
        && data.Amount > 0 && decimal.Round(data.Amount, 2) == data.Amount
        && (data.Description is null || !data.Description.Any(char.IsControl));

    private static bool IsValidQuery(PixChargeQuery query, ProviderConnection connection) =>
        query.TenantId != Guid.Empty && query.ConnectionId == connection.Id && connection.TenantId == query.TenantId
        && connection.Provider == "Asaas" && connection.Group == "PaymentProvider"
        && string.Equals(query.ExpectedProvider, "Asaas", StringComparison.Ordinal)
        && Reference(query.ProviderReference);

    private static bool Reference(string value) => value is { Length: > 0 and <= 100 }
        && value.All(char.IsAsciiLetterOrDigit);

    private static PixChargeCreationOutcome ToCreationOutcome(AsaasApiOutcome outcome) => outcome switch
    {
        AsaasApiOutcome.Rejected => PixChargeCreationOutcome.Rejected,
        AsaasApiOutcome.ConfigurationError => PixChargeCreationOutcome.Rejected,
        _ => PixChargeCreationOutcome.Rejected
    };

    private static bool IsProviderFailure(Exception exception) => exception is HttpRequestException or IOException
        or TimeoutException or System.Text.Json.JsonException or InvalidOperationException;
}
