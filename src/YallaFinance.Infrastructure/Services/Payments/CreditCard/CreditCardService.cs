using System.Data.Common;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Application.Services.Payments.CreditCard;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.CreditCard;

namespace YallaFinance.Infrastructure.Services.Payments.CreditCard;

internal sealed class CreditCardService(
    ICompanyConnectionRepository connections,
    IProviderResolver<ICreditCardPaymentProvider> providers) : ICreditCardService
{
    public Task<CreditCardResult<CreditCardPage<CreditCardCustomer>>> FindCustomersAsync(CreditCardCustomerQuery query, CancellationToken cancellationToken) =>
        CallProviderAsync(query?.Context, query is not null && ValidPage(query.Reference, query.Offset, query.Limit),
            (provider, connection, token) => provider.FindCustomersAsync(query!.Reference, query.Offset, query.Limit, connection, token),
            page => ValidPageResult(page, query!.Offset, query.Limit, customer => ValidCustomer(customer) && customer.Reference == query.Reference),
            page => new CreditCardPage<CreditCardCustomer>(page.Items.Select(Customer).ToArray(), page.HasMore, page.Offset, page.Limit), cancellationToken);

    public Task<CreditCardResult<CreditCardCustomer>> CreateCustomerAsync(CreateCreditCardCustomerData data, CancellationToken cancellationToken) =>
        CallProviderAsync(data?.Context, data is not null && data.OperationId != Guid.Empty && Reference(data.Reference)
            && Text(data.Name) && Document(data.Document) && OptionalText(data.Email) && OptionalText(data.Phone),
            (provider, connection, token) => provider.CreateCustomerAsync(new(data!.Name, data.Document, data.Email, data.Phone, data.Reference), connection, token),
            customer => ValidCustomer(customer) && customer.Reference == data!.Reference && customer.Document == data.Document,
            Customer, cancellationToken);

    public Task<CreditCardResult<CreditCardCharge>> CreateChargeAsync(CreateCreditCardChargeData data, CancellationToken cancellationToken) =>
        CallProviderAsync(data?.Context, data is not null && data.OperationId != Guid.Empty && Identifier(data.CustomerId)
            && Amount(data.Amount) && data.DueDate != default && Reference(data.Reference)
            && (data.Description is null || (OptionalText(data.Description) && data.Description.Length <= 500)),
            (provider, connection, token) => provider.CreateChargeAsync(new(data!.CustomerId, data.Amount, data.DueDate, data.Reference, data.Description), connection, token),
            charge => ValidCharge(charge) && charge.CustomerId == data!.CustomerId && charge.Amount == data.Amount && charge.Reference == data.Reference,
            Charge, cancellationToken);

    public Task<CreditCardResult<CreditCardCharge>> PayChargeAsync(PayCreditCardChargeData data, CancellationToken cancellationToken) =>
        CallProviderAsync(data?.Context, data is not null && data.OperationId != Guid.Empty && Identifier(data.ChargeId)
            && Identifier(data.ExpectedCustomerId) && Amount(data.ExpectedAmount) && Reference(data.ExpectedReference) && ValidPayment(data.Payment),
            (provider, connection, token) => provider.PayChargeAsync(data!.ChargeId, new(
                data.Payment.HolderName, data.Payment.Number, data.Payment.ExpiryMonth, data.Payment.ExpiryYear,
                data.Payment.SecurityCode, data.Payment.PayerName, data.Payment.Email, data.Payment.Document,
                data.Payment.PostalCode, data.Payment.AddressNumber, data.Payment.Phone,
                data.Payment.AddressComplement, data.Payment.MobilePhone), connection, token),
            charge => ValidCharge(charge) && charge.Id == data!.ChargeId && charge.CustomerId == data.ExpectedCustomerId
                && charge.Amount == data.ExpectedAmount && charge.Reference == data.ExpectedReference,
            Charge, cancellationToken);

    public Task<CreditCardResult<CreditCardCharge>> GetChargeAsync(CreditCardChargeQuery query, CancellationToken cancellationToken) =>
        CallProviderAsync(query?.Context, query is not null && Identifier(query.ChargeId),
            (provider, connection, token) => provider.GetChargeAsync(query!.ChargeId, connection, token),
            charge => ValidCharge(charge) && charge.Id == query!.ChargeId, Charge, cancellationToken);

    public Task<CreditCardResult<CreditCardPage<CreditCardCharge>>> FindChargesAsync(CreditCardChargeSearch query, CancellationToken cancellationToken) =>
        CallProviderAsync(query?.Context, query is not null && ValidPage(query.Reference, query.Offset, query.Limit),
            (provider, connection, token) => provider.FindChargesAsync(query!.Reference, query.Offset, query.Limit, connection, token),
            page => ValidPageResult(page, query!.Offset, query.Limit, charge => ValidCharge(charge) && charge.Reference == query.Reference),
            page => new CreditCardPage<CreditCardCharge>(page.Items.Select(Charge).ToArray(), page.HasMore, page.Offset, page.Limit), cancellationToken);

    private async Task<CreditCardResult<TResult>> CallProviderAsync<TProvider, TResult>(
        CreditCardConnectionContext? context,
        bool validInput,
        Func<ICreditCardPaymentProvider, ProviderConnection, CancellationToken, Task<CardProviderResult<TProvider>>> operation,
        Func<TProvider, bool> validResult,
        Func<TProvider, TResult> project,
        CancellationToken cancellationToken) where TProvider : class where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!validInput || context is null || context.TenantId == Guid.Empty || context.ConnectionId == Guid.Empty || !Text(context.ExpectedProvider))
            return new(CreditCardOperationOutcome.Rejected, null, "Validation");

        var delegated = false;
        try
        {
            var record = await connections.GetRuntimeAsync(context.TenantId, context.ConnectionId, cancellationToken);
            if (record is null || record.Id != context.ConnectionId || record.TenantId != context.TenantId
                || record.Group != "PaymentProvider" || !record.Enabled)
                return new(CreditCardOperationOutcome.ConfigurationError, null, "ConnectionUnavailable");
            if (!string.Equals(record.Provider, context.ExpectedProvider, StringComparison.Ordinal))
                return new(CreditCardOperationOutcome.ConfigurationError, null, "ProviderChanged");
            var provider = providers.Resolve(record.Provider);
            var connection = new ProviderConnection(record.Id, record.TenantId, record.Provider, record.Name, record.Enabled, record.Group);
            cancellationToken.ThrowIfCancellationRequested();
            delegated = true;
            var result = await operation(provider, connection, cancellationToken);
            if (result is null) return Unknown<TResult>();
            if (result.Outcome != CardProviderOutcome.Completed)
            {
                var outcome = result.Outcome switch
                {
                    CardProviderOutcome.Rejected => CreditCardOperationOutcome.Rejected,
                    CardProviderOutcome.ConfigurationError => CreditCardOperationOutcome.ConfigurationError,
                    _ => CreditCardOperationOutcome.Indeterminate
                };
                return new(outcome, null, SafeErrorCode(result.ErrorCode) ?? (outcome switch
                {
                    CreditCardOperationOutcome.Rejected => "ProviderRejected",
                    CreditCardOperationOutcome.ConfigurationError => "ProviderConfigurationError",
                    _ => "ProviderOutcomeUnknown"
                })) { Diagnostic = result.Diagnostic };
            }
            if (result.Value is null || !validResult(result.Value)) return Unknown<TResult>(result.Diagnostic);
            return new(CreditCardOperationOutcome.Completed, project(result.Value), SafeErrorCode(result.ErrorCode)) { Diagnostic = result.Diagnostic };
        }
        catch (IntegrationConfigurationException exception)
        {
            return new(CreditCardOperationOutcome.ConfigurationError, null, SafeErrorCode(exception.Code) ?? "InvalidProviderConfiguration");
        }
        catch (OperationCanceledException) when (!delegated && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is DbException or IOException or HttpRequestException or TimeoutException or OperationCanceledException or InvalidOperationException)
        {
            return delegated ? Unknown<TResult>() : new(CreditCardOperationOutcome.ConfigurationError, null, "ConnectionUnavailable");
        }
    }

    public Task<CreditCardResult<CreditCardChargeCancellation>> CancelChargeAsync(CreditCardChargeQuery query, CancellationToken cancellationToken) =>
        CallProviderAsync(query?.Context, query is not null && Identifier(query.ChargeId),
            (provider, connection, token) => provider.CancelChargeAsync(query!.ChargeId, connection, token),
            result => result.Id == query!.ChargeId && result.Deleted, result => result, cancellationToken);

    private static CreditCardResult<T> Unknown<T>(CreditCardProviderDiagnostic? diagnostic = null) =>
        new(CreditCardOperationOutcome.Indeterminate, default, "ProviderOutcomeUnknown") { Diagnostic = diagnostic };
    private static CreditCardCustomer Customer(CardCustomer customer) => new(customer.Id, customer.Reference, customer.Document);
    private static CreditCardCharge Charge(CardCharge charge) => new(charge.Id, charge.CustomerId, charge.Amount, charge.Reference, charge.State switch
    {
        CardPaymentState.Pending => CreditCardPaymentState.Pending,
        CardPaymentState.UnderReview => CreditCardPaymentState.UnderReview,
        CardPaymentState.Confirmed => CreditCardPaymentState.Confirmed,
        CardPaymentState.Received => CreditCardPaymentState.Received,
        CardPaymentState.Authorized => CreditCardPaymentState.Authorized,
        CardPaymentState.Overdue => CreditCardPaymentState.Overdue,
        CardPaymentState.Refunded => CreditCardPaymentState.Refunded,
        CardPaymentState.RefundPending => CreditCardPaymentState.RefundPending,
        CardPaymentState.ChargebackRequested => CreditCardPaymentState.ChargebackRequested,
        CardPaymentState.ChargebackDispute => CreditCardPaymentState.ChargebackDispute,
        CardPaymentState.AwaitingChargebackReversal => CreditCardPaymentState.AwaitingChargebackReversal,
        _ => CreditCardPaymentState.Unknown
    }, charge.ProviderStatus)
    {
        Deleted = charge.Deleted,
        ProviderPayloadJson = charge.ProviderPayloadJson
    };

    private static bool ValidCustomer(CardCustomer? customer) => customer is not null && Identifier(customer.Id)
        && (customer.Reference is null || Reference(customer.Reference)) && (customer.Document is null || Document(customer.Document));
    private static bool ValidCharge(CardCharge? charge) => charge is not null && Identifier(charge.Id) && Identifier(charge.CustomerId)
        && Amount(charge.Amount) && (charge.Reference is null || Reference(charge.Reference)) && Enum.IsDefined(charge.State)
        && charge.ProviderStatus is { Length: > 0 and <= 80 } && charge.ProviderStatus.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
    private static bool ValidPageResult<T>(CardProviderPage<T>? page, int offset, int limit, Func<T, bool> validItem) =>
        page is not null && page.Items is not null && page.Offset == offset && page.Limit == limit && page.Items.Count <= limit
        && (!page.HasMore || page.Items.Count > 0) && page.Items.All(validItem);
    private static bool ValidPayment(CreditCardPaymentInput? payment) => payment is not null && Text(payment.HolderName)
        && Digits(payment.Number, 13, 19) && Digits(payment.SecurityCode, 3, 4) && Digits(payment.ExpiryMonth, 2, 2)
        && int.TryParse(payment.ExpiryMonth, out var month) && month is >= 1 and <= 12 && Digits(payment.ExpiryYear, 4, 4)
        && Text(payment.PayerName) && Text(payment.Email) && Document(payment.Document) && Digits(payment.PostalCode, 8, 8)
        && Text(payment.AddressNumber) && Digits(payment.Phone, 10, 11) && OptionalText(payment.AddressComplement)
        && (payment.MobilePhone is null || Digits(payment.MobilePhone, 10, 11));
    private static bool ValidPage(string reference, int offset, int limit) => Reference(reference) && offset >= 0 && limit is >= 1 and <= 100;
    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
    private static bool OptionalText(string? value) => value is null || !value.Any(char.IsControl);
    private static bool Reference(string? value) => Text(value) && value!.Length <= 200;
    private static bool Identifier(string? value) => value is { Length: > 0 and <= 100 } && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
    private static bool Amount(decimal value) => value > 0 && decimal.Round(value, 2) == value;
    private static bool Digits(string? value, int minimum, int maximum) => value is not null && value.Length >= minimum && value.Length <= maximum && value.All(char.IsAsciiDigit);
    private static bool Document(string? value) => Digits(value, 11, 11) || Digits(value, 14, 14);
    private static string? SafeErrorCode(string? value) => value is { Length: > 0 and <= 80 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-') ? value : null;
}
