using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers.Payments.CreditCard;
using YallaFinance.Infrastructure.Providers.Asaas.Payments;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments.CreditCard;

internal sealed class AsaasCreditCardProvider : ICreditCardPaymentProvider
{
    private readonly AsaasApiTransport apiTransport;
    private readonly AsaasPaymentOperations paymentOperations;

    internal AsaasCreditCardProvider(AsaasConfigurationReader configuration, AsaasClient client,
        ILogger<AsaasCreditCardProvider> logger)
        : this(new AsaasApiTransport(configuration, client, logger))
    {
    }

    private AsaasCreditCardProvider(AsaasApiTransport apiTransport)
        : this(apiTransport, new AsaasPaymentOperations(apiTransport))
    {
    }

    public AsaasCreditCardProvider(AsaasApiTransport apiTransport, AsaasPaymentOperations paymentOperations)
    {
        this.apiTransport = apiTransport;
        this.paymentOperations = paymentOperations;
    }

    public string Provider => "Asaas";

    public Task<CardProviderResult<CardProviderPage<CardCustomer>>> FindCustomersAsync(
        string reference, int offset, int limit, ProviderConnection connection, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, SearchPath("customers", reference, offset, limit), null, connection,
            json => ReadPage(json, ReadCustomer, offset, limit), cancellationToken);

    public Task<CardProviderResult<CardCustomer>> CreateCustomerAsync(
        CreateCardCustomer customer, ProviderConnection connection, CancellationToken cancellationToken)
    {
        if (!Text(customer.Name) || !HasCpfOrCnpjDigits(customer.Document) || !IsRecoverableReference(customer.Reference))
            return Invalid<CardCustomer>(cancellationToken);
        var phone = NormalizePhone(customer.Phone);
        if (customer.Phone is not null && phone is null) return Invalid<CardCustomer>(cancellationToken);
        return SendAsync(HttpMethod.Post, "customers", new
        {
            name = customer.Name, cpfCnpj = customer.Document, email = customer.Email,
            phone = phone?.Length == 10 ? phone : null, mobilePhone = phone?.Length == 11 ? phone : null,
            externalReference = customer.Reference, notificationDisabled = true
        }, connection, json =>
        {
            var result = ReadCustomer(json);
            if (result.Reference != customer.Reference || result.Document != customer.Document)
                throw new JsonException();
            return result;
        }, cancellationToken);
    }

    public Task<CardProviderResult<CardCharge>> CreateChargeAsync(
        CreateCardCharge charge, ProviderConnection connection, CancellationToken cancellationToken)
    {
        var request = new AsaasCreatePaymentCharge(charge.CustomerId, charge.Amount, charge.DueDate,
            charge.Reference, charge.Description);
        return MapPaymentResultAsync(paymentOperations.CreateChargeAsync(request,
            AsaasBillingType.CreditCard, connection, cancellationToken), result =>
        {
            if (result.CustomerId != charge.CustomerId || result.Amount != charge.Amount || result.Reference != charge.Reference)
                throw new JsonException();
            return ToCardCharge(result);
        });
    }

    public Task<CardProviderResult<CardCharge>> PayChargeAsync(
        string chargeId, CardPaymentInput payment, ProviderConnection connection, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(payment.Phone);
        var mobile = NormalizePhone(payment.MobilePhone);
        if (!Identifier(chargeId) || !Text(payment.HolderName) || !Digits(payment.Number, 13, 19)
            || !Digits(payment.SecurityCode, 3, 4) || !Digits(payment.ExpiryMonth, 2, 2)
            || !int.TryParse(payment.ExpiryMonth, out var month) || month is < 1 or > 12
            || !Digits(payment.ExpiryYear, 4, 4) || !Text(payment.PayerName) || !Text(payment.Email)
            || !HasCpfOrCnpjDigits(payment.Document) || !Digits(payment.PostalCode, 8, 8)
            || !Text(payment.AddressNumber) || phone is null || payment.MobilePhone is not null && mobile?.Length != 11)
            return Invalid<CardCharge>(cancellationToken);
        return SendAsync(HttpMethod.Post, $"payments/{chargeId}/payWithCreditCard", new
        {
            creditCard = new
            {
                holderName = payment.HolderName, number = payment.Number,
                expiryMonth = payment.ExpiryMonth, expiryYear = payment.ExpiryYear, ccv = payment.SecurityCode
            },
            creditCardHolderInfo = new
            {
                name = payment.PayerName, email = payment.Email, cpfCnpj = payment.Document,
                postalCode = payment.PostalCode, addressNumber = payment.AddressNumber,
                phone = phone.Length == 10 ? phone : null,
                addressComplement = payment.AddressComplement, mobilePhone = mobile ?? (phone.Length == 11 ? phone : null)
            }
        }, connection, json => ToCardCharge(AsaasPaymentOperations.ReadChargeWithId(
            json, chargeId, AsaasBillingType.CreditCard)), cancellationToken, [payment.Number, payment.SecurityCode]);
    }

    public Task<CardProviderResult<CardCharge>> GetChargeAsync(string chargeId, ProviderConnection connection, CancellationToken cancellationToken) =>
        MapPaymentResultAsync(paymentOperations.GetChargeAsync(chargeId, AsaasBillingType.CreditCard,
            connection, cancellationToken), ToCardCharge);

    public Task<CardProviderResult<YallaFinance.Application.Services.Payments.CreditCard.Contracts.CreditCardChargeCancellation>> CancelChargeAsync(
        string chargeId, ProviderConnection connection, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, Identifier(chargeId) ? $"payments/{chargeId}" : null, null, connection, json =>
        {
            if (RequiredId(json, "id") != chargeId || json.GetProperty("deleted").ValueKind != JsonValueKind.True)
                throw new JsonException();
            return new YallaFinance.Application.Services.Payments.CreditCard.Contracts.CreditCardChargeCancellation(chargeId, true);
        }, cancellationToken);

    public Task<CardProviderResult<CardChargeStatus>> GetChargeStatusAsync(string chargeId, ProviderConnection connection, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, Identifier(chargeId) ? $"payments/{chargeId}/status" : null, null,
            connection, json =>
            {
                var status = RequiredStatus(json);
                return new CardChargeStatus(chargeId, MapStatus(status), status);
            }, cancellationToken);

    public Task<CardProviderResult<CardProviderPage<CardCharge>>> FindChargesAsync(
        string reference, int offset, int limit, ProviderConnection connection, CancellationToken cancellationToken) =>
        MapPaymentResultAsync(paymentOperations.FindChargesAsync(reference, AsaasBillingType.CreditCard,
            offset, limit, connection, cancellationToken), page => new CardProviderPage<CardCharge>(
                page.Items.Select(ToCardCharge).ToArray(), page.HasMore, page.Offset, page.Limit));

    private static async Task<CardProviderResult<TOutput>> MapPaymentResultAsync<TInput, TOutput>(
        Task<AsaasApiResult<TInput>> operation,
        Func<TInput, TOutput> map)
    {
        var result = await operation;
        var outcome = result.Outcome switch
        {
            AsaasApiOutcome.Completed => CardProviderOutcome.Completed,
            AsaasApiOutcome.Rejected => CardProviderOutcome.Rejected,
            AsaasApiOutcome.ConfigurationError => CardProviderOutcome.ConfigurationError,
            _ => CardProviderOutcome.Indeterminate
        };
        var diagnostic = result.Diagnostic is null ? null : new CreditCardProviderDiagnostic(
            result.Diagnostic.HttpStatusCode,
            result.Diagnostic.Errors.Select(error => new CreditCardProviderError(error.Code, error.Description)).ToArray());
        TOutput? value;
        try
        {
            value = result.Value is null ? default : map(result.Value);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException
            or OverflowException or KeyNotFoundException)
        {
            return new(CardProviderOutcome.Indeterminate, default, "UnresolvedProviderResult")
                { Diagnostic = diagnostic };
        }
        var errorCode = result.ErrorCode;
        if (outcome == CardProviderOutcome.Completed && HasUnknownStatus(value))
            errorCode = "UnknownProviderStatus";
        return new(outcome, value, errorCode) { Diagnostic = diagnostic };
    }

    private static CardCharge ToCardCharge(AsaasPaymentCharge charge) => new(
        charge.Id, charge.CustomerId, charge.Amount, charge.Reference, MapStatus(charge.Status), charge.Status)
    {
        Deleted = charge.Deleted,
        ProviderPayloadJson = charge.ProviderPayloadJson
    };

    private Task<CardProviderResult<T>> SendAsync<T>(HttpMethod method, string? path, object? body,
        ProviderConnection connection, Func<JsonElement, T> read, CancellationToken cancellationToken,
        IReadOnlyList<string>? sensitiveValues = null) =>
        MapPaymentResultAsync(apiTransport.SendAsync(method, path, body, connection, read,
            cancellationToken, sensitiveValues), value => value);

    private static Task<CardProviderResult<T>> Invalid<T>(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CardProviderResult<T>(CardProviderOutcome.Rejected, default, "Validation"));
    }

    private static string? SearchPath(string resource, string reference, int offset, int limit) =>
        IsRecoverableReference(reference) && offset >= 0 && limit is >= 1 and <= 100
            ? $"{resource}?externalReference={Uri.EscapeDataString(reference)}&offset={offset.ToString(CultureInfo.InvariantCulture)}&limit={limit.ToString(CultureInfo.InvariantCulture)}" : null;

    private static CardProviderPage<T> ReadPage<T>(JsonElement json, Func<JsonElement, T> read, int offset, int limit)
    {
        var items = json.GetProperty("data");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > limit)
            throw new JsonException();
        return new(items.EnumerateArray().Select(read).ToArray(), json.GetProperty("hasMore").GetBoolean(), offset, limit);
    }

    private static CardCustomer ReadCustomer(JsonElement json)
    {
        var id = RequiredId(json, "id");
        return new(id, OptionalText(json, "externalReference"), OptionalText(json, "cpfCnpj"));
    }

    private static string RequiredId(JsonElement json, string key)
    {
        var value = json.GetProperty(key).GetString();
        return Identifier(value) ? value! : throw new JsonException();
    }

    private static string? NormalizePhone(string? value)
    {
        if (value is null || value.Any(character => !char.IsAsciiDigit(character) && character is not ('+' or '-' or '(' or ')' or ' '))) return null;
        var phone = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (phone.Length is 12 or 13 && phone.StartsWith("55", StringComparison.Ordinal)) phone = phone[2..];
        return phone.Length is 10 or 11 ? phone : null;
    }

    private static string RequiredStatus(JsonElement json)
    {
        var status = json.GetProperty("status").GetString();
        return status is { Length: > 0 and <= 80 }
            && status.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            ? status : throw new JsonException();
    }

    private static string? OptionalText(JsonElement json, string key) =>
        json.TryGetProperty(key, out var property) && property.ValueKind != JsonValueKind.Null ? property.GetString() : null;

    private static CardPaymentState MapStatus(string status) => status switch
    {
        "PENDING" => CardPaymentState.Pending,
        "AWAITING_RISK_ANALYSIS" => CardPaymentState.UnderReview,
        "CONFIRMED" => CardPaymentState.Confirmed,
        "RECEIVED" => CardPaymentState.Received,
        "AUTHORIZED" => CardPaymentState.Authorized,
        "OVERDUE" => CardPaymentState.Overdue,
        "REFUNDED" => CardPaymentState.Refunded,
        "REFUND_REQUESTED" or "REFUND_IN_PROGRESS" => CardPaymentState.RefundPending,
        "CHARGEBACK_REQUESTED" => CardPaymentState.ChargebackRequested,
        "CHARGEBACK_DISPUTE" => CardPaymentState.ChargebackDispute,
        "AWAITING_CHARGEBACK_REVERSAL" => CardPaymentState.AwaitingChargebackReversal,
        _ => CardPaymentState.Unknown
    };

    private static bool HasUnknownStatus<T>(T value) => value switch
    {
        CardCharge charge => charge.State == CardPaymentState.Unknown,
        CardChargeStatus status => status.State == CardPaymentState.Unknown,
        CardProviderPage<CardCharge> page => page.Items.Any(charge => charge.State == CardPaymentState.Unknown),
        _ => false
    };

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
    private static bool IsRecoverableReference(string? value) => Text(value) && value!.Length <= 200;
    private static bool Identifier(string? value) => value is { Length: > 0 and <= 100 }
        && value.All(character => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');
    private static bool Digits(string? value, int minimum, int maximum) => value is not null
        && value.Length >= minimum && value.Length <= maximum && value.All(character => character is >= '0' and <= '9');
    private static bool HasCpfOrCnpjDigits(string? value) => Digits(value, 11, 11) || Digits(value, 14, 14);
}
