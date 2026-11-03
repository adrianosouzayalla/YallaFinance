using System.Data.Common;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Application.Services.Payments.Boleto;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.Boleto;

namespace YallaFinance.Infrastructure.Services.Payments.Boleto;

internal sealed class BoletoService(
    ICompanyConnectionRepository connections,
    IProviderResolver<IBoletoPaymentProvider> providers) : IBoletoService
{
    public Task<BoletoOperationResult<BoletoCharge>> CreateAsync(
        CreateBoletoChargeData data,
        CancellationToken cancellationToken) => CallProviderAsync(
            data?.Context,
            data is not null && data.OperationId != Guid.Empty && Reference(data.ProviderReference)
                && Amount(data.Amount) && data.DueDate != default && ValidPayer(data.Payer)
                && (data.Description is null || OptionalText(data.Description) && data.Description.Length <= 500),
            creating: true,
            (provider, connection, token) => provider.CreateAsync(data!, connection, token),
            charge => ValidCharge(charge, data!.ProviderReference, data.Amount),
            cancellationToken);

    public Task<BoletoOperationResult<BoletoCharge>> GetAsync(
        BoletoChargeQuery query,
        CancellationToken cancellationToken) => CallProviderAsync(
            query?.Context,
            query is not null && query.OperationId != Guid.Empty && Reference(query.ProviderReference)
                && (query.ProviderPaymentId is null || Identifier(query.ProviderPaymentId)),
            creating: false,
            (provider, connection, token) => provider.GetAsync(query!, connection, token),
            charge => ValidCharge(charge, query!.ProviderReference, expectedAmount: null),
            cancellationToken);

    private async Task<BoletoOperationResult<BoletoCharge>> CallProviderAsync(
        BoletoConnectionContext? context,
        bool validInput,
        bool creating,
        Func<IBoletoPaymentProvider, ProviderConnection, CancellationToken, Task<BoletoProviderResult<BoletoCharge>>> operation,
        Func<BoletoCharge, bool> validResult,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!validInput || context is null || context.TenantId == Guid.Empty || context.ConnectionId == Guid.Empty
            || !OptionalText(context.ExpectedProvider))
            return new(BoletoOperationOutcome.Rejected, null, "Validation");

        var delegated = false;
        try
        {
            var record = await connections.GetRuntimeAsync(context.TenantId, context.ConnectionId, cancellationToken);
            if (record is null || record.Id != context.ConnectionId || record.TenantId != context.TenantId
                || record.Group != "PaymentProvider" || creating && !record.Enabled)
                return new(BoletoOperationOutcome.ConfigurationError, null, "ConnectionUnavailable");
            if (!string.Equals(record.Provider, context.ExpectedProvider, StringComparison.Ordinal))
                return new(BoletoOperationOutcome.ConfigurationError, null, "ProviderChanged");

            var provider = providers.Resolve(record.Provider);
            var connection = new ProviderConnection(record.Id, record.TenantId, record.Provider,
                record.Name, record.Enabled, record.Group);
            cancellationToken.ThrowIfCancellationRequested();
            delegated = true;
            var result = await operation(provider, connection, cancellationToken);
            if (result is null)
                return Unknown();
            if (result.Outcome != BoletoProviderOutcome.Completed)
                return new(MapOutcome(result.Outcome), null, SafeErrorCode(result.ErrorCode) ?? "ProviderOutcomeUnknown")
                {
                    FailureMessage = result.FailureMessage
                };
            if (result.Value is null || !validResult(result.Value))
                return Unknown();
            return new(BoletoOperationOutcome.Completed, result.Value, SafeErrorCode(result.ErrorCode));
        }
        catch (IntegrationConfigurationException exception)
        {
            return new(BoletoOperationOutcome.ConfigurationError, null,
                SafeErrorCode(exception.Code) ?? "InvalidProviderConfiguration");
        }
        catch (OperationCanceledException) when (!delegated && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is DbException or IOException or HttpRequestException
            or TimeoutException or OperationCanceledException or InvalidOperationException)
        {
            return delegated ? Unknown() : new(BoletoOperationOutcome.ConfigurationError, null, "ConnectionUnavailable");
        }
    }

    private static BoletoOperationResult<BoletoCharge> Unknown() =>
        new(BoletoOperationOutcome.Indeterminate, null, "ProviderOutcomeUnknown");

    private static BoletoOperationOutcome MapOutcome(BoletoProviderOutcome outcome) => outcome switch
    {
        BoletoProviderOutcome.Rejected => BoletoOperationOutcome.Rejected,
        BoletoProviderOutcome.ConfigurationError => BoletoOperationOutcome.ConfigurationError,
        BoletoProviderOutcome.NotFound => BoletoOperationOutcome.NotFound,
        _ => BoletoOperationOutcome.Indeterminate
    };

    private static bool ValidCharge(BoletoCharge? charge, string reference, decimal? expectedAmount) =>
        charge is not null && Identifier(charge.ProviderPaymentId) && Amount(charge.Amount)
        && (expectedAmount is null || charge.Amount == expectedAmount)
        && charge.ProviderReference == reference && Enum.IsDefined(charge.State)
        && ProviderStatus(charge.ProviderStatus)
        && OptionalText(charge.IdentificationField) && OptionalText(charge.Barcode)
        && OptionalText(charge.BankSlipNumber) && OptionalText(charge.BankSlipUrl)
        && OptionalText(charge.InvoiceUrl) && OptionalText(charge.ProviderPayloadJson);

    private static bool ValidPayer(BoletoPayer? payer) => payer is not null && OptionalText(payer.Name)
        && Document(payer.TaxId) && (payer.CustomerId is null || payer.CustomerId != Guid.Empty);

    private static bool Document(string? value) => value is { Length: 11 or 14 } && value.All(char.IsAsciiDigit);
    private static bool Amount(decimal value) => value > 0 && decimal.Round(value, 2) == value;
    private static bool Reference(string? value) => OptionalText(value) && value!.Length <= 200;
    private static bool Identifier(string? value) => value is { Length: > 0 and <= 100 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
    private static bool ProviderStatus(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
    private static bool OptionalText(string? value) => value is null || !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
    private static string? SafeErrorCode(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-') ? value : null;
}
