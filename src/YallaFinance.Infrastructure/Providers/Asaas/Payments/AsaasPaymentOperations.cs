using System.Globalization;
using System.Text.Json;
using YallaFinance.Infrastructure.Providers.Asaas;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments;

internal sealed class AsaasPaymentOperations(AsaasApiTransport transport)
{
    public Task<AsaasApiResult<AsaasPaymentCharge>> CreateChargeAsync(
        AsaasCreatePaymentCharge charge,
        AsaasBillingType billingType,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (!Identifier(charge.CustomerId) || !Reference(charge.Reference) || charge.Amount <= 0
            || decimal.Round(charge.Amount, 2) != charge.Amount || charge.DueDate == default
            || charge.Description?.Length > 500)
            return Invalid<AsaasPaymentCharge>(cancellationToken);

        return SendAsync(HttpMethod.Post, "payments", new
        {
            customer = charge.CustomerId,
            billingType = billingType.ToApiValue(),
            value = charge.Amount,
            dueDate = charge.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            externalReference = charge.Reference,
            description = charge.Description
        }, connection, json => ReadCharge(json, billingType), cancellationToken);
    }

    public Task<AsaasApiResult<AsaasPaymentCharge>> GetChargeAsync(
        string chargeId,
        AsaasBillingType billingType,
        ProviderConnection connection,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, Identifier(chargeId) ? $"payments/{chargeId}" : null, null,
            connection, json => ReadChargeWithId(json, chargeId, billingType), cancellationToken);

    public Task<AsaasApiResult<AsaasPaymentPage<AsaasPaymentCharge>>> FindChargesAsync(
        string reference,
        AsaasBillingType billingType,
        int offset,
        int limit,
        ProviderConnection connection,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, SearchPath(reference, offset, limit), null, connection,
            json => ReadPage(json, item => ReadCharge(item, billingType), offset, limit), cancellationToken);

    public Task<AsaasApiResult<AsaasBoletoBillingInfo>> GetBoletoBillingInfoAsync(
        string paymentId,
        ProviderConnection connection,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, Identifier(paymentId) ? $"payments/{paymentId}/billingInfo" : null, null,
            connection, ReadBoletoBillingInfo, cancellationToken);

    private Task<AsaasApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string? path,
        object? body,
        ProviderConnection connection,
        Func<JsonElement, T> read,
        CancellationToken cancellationToken) =>
        transport.SendAsync(method, path, body, connection, read, cancellationToken);

    private static Task<AsaasApiResult<T>> Invalid<T>(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AsaasApiResult<T>(AsaasApiOutcome.Rejected, default, "Validation"));
    }

    private static string? SearchPath(string reference, int offset, int limit) =>
        Reference(reference) && offset >= 0 && limit is >= 1 and <= 100
            ? $"payments?externalReference={Uri.EscapeDataString(reference)}&offset={offset.ToString(CultureInfo.InvariantCulture)}&limit={limit.ToString(CultureInfo.InvariantCulture)}"
            : null;

    private static AsaasPaymentPage<T> ReadPage<T>(JsonElement json, Func<JsonElement, T> read, int offset, int limit)
    {
        var items = json.GetProperty("data");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > limit)
            throw new JsonException();
        return new(items.EnumerateArray().Select(read).ToArray(), json.GetProperty("hasMore").GetBoolean(), offset, limit);
    }

    internal static AsaasPaymentCharge ReadChargeWithId(JsonElement json, string chargeId, AsaasBillingType billingType)
    {
        var charge = ReadCharge(json, billingType);
        return charge.Id == chargeId ? charge : throw new JsonException();
    }

    internal static AsaasPaymentCharge ReadCharge(JsonElement json, AsaasBillingType expectedBillingType)
    {
        var id = RequiredId(json, "id");
        var customerId = RequiredId(json, "customer");
        var billingType = json.GetProperty("billingType").GetString();
        if (!string.Equals(billingType, expectedBillingType.ToApiValue(), StringComparison.Ordinal))
            throw new JsonException();
        var amount = json.GetProperty("value").GetDecimal();
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new JsonException();
        var status = RequiredStatus(json);
        return new(id, customerId, amount, OptionalText(json, "externalReference"), billingType!, status,
            json.TryGetProperty("deleted", out var deleted) && deleted.ValueKind == JsonValueKind.True,
            SanitizeProviderPayload(json), OptionalText(json, "invoiceUrl"));
    }

    private static AsaasBoletoBillingInfo ReadBoletoBillingInfo(JsonElement json)
    {
        if (!json.TryGetProperty("bankSlip", out var bankSlip) || bankSlip.ValueKind != JsonValueKind.Object)
            throw new JsonException();
        var identificationField = OptionalText(bankSlip, "identificationField");
        if (string.IsNullOrWhiteSpace(identificationField))
            throw new JsonException();
        return new(
            identificationField,
            OptionalText(bankSlip, "barCode"),
            OptionalText(bankSlip, "nossoNumero"),
            OptionalText(bankSlip, "bankSlipUrl"),
            SanitizeProviderPayload(bankSlip));
    }

    internal static string SanitizeProviderPayload(JsonElement providerPayload)
    {
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
            WriteSanitizedProviderPayload(writer, providerPayload, null);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static void WriteSanitizedProviderPayload(Utf8JsonWriter writer, JsonElement value, string? parentPropertyName)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject())
            {
                if (IsSensitiveProviderPayloadProperty(property.Name, parentPropertyName, property.Value))
                    continue;
                writer.WritePropertyName(property.Name);
                WriteSanitizedProviderPayload(writer, property.Value, property.Name);
            }
            writer.WriteEndObject();
            return;
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray())
                WriteSanitizedProviderPayload(writer, item, parentPropertyName);
            writer.WriteEndArray();
            return;
        }
        value.WriteTo(writer);
    }

    private static bool IsSensitiveProviderPayloadProperty(string propertyName, string? parentPropertyName, JsonElement value)
    {
        var normalizedPropertyName = NormalizePropertyName(propertyName);
        if (normalizedPropertyName.Contains("token", StringComparison.Ordinal)
            || normalizedPropertyName is "ccv" or "cvv" or "securitycode" or "authorization" or "accesstoken"
                or "apikey" or "pan" or "primaryaccountnumber")
            return true;
        var normalizedParentPropertyName = parentPropertyName is null ? string.Empty : NormalizePropertyName(parentPropertyName);
        if (normalizedParentPropertyName.Contains("creditcard", StringComparison.Ordinal)
            && normalizedPropertyName == "number")
            return true;
        if (normalizedPropertyName is "creditcardnumber" or "cardnumber")
            return value.ValueKind != JsonValueKind.String || value.GetString() is { Length: > 4 };
        return false;
    }

    private static string NormalizePropertyName(string value) =>
        new(value.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string RequiredId(JsonElement json, string key)
    {
        var value = json.GetProperty(key).GetString();
        return Identifier(value) ? value! : throw new JsonException();
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

    private static bool Identifier(string? value) => value is { Length: > 0 and <= 100 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    private static bool Reference(string? value) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= 200 && !value.Any(char.IsControl);

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
}

internal enum AsaasBillingType
{
    CreditCard,
    Pix,
    Boleto
}

internal static class AsaasBillingTypeExtensions
{
    public static string ToApiValue(this AsaasBillingType billingType) => billingType switch
    {
        AsaasBillingType.CreditCard => "CREDIT_CARD",
        AsaasBillingType.Pix => "PIX",
        AsaasBillingType.Boleto => "BOLETO",
        _ => throw new ArgumentOutOfRangeException(nameof(billingType))
    };
}

internal enum AsaasApiOutcome { Completed, Rejected, Indeterminate, ConfigurationError }

internal sealed record AsaasApiResult<T>(
    AsaasApiOutcome Outcome,
    T? Value,
    string? ErrorCode = null,
    AsaasApiDiagnostic? Diagnostic = null);

internal sealed record AsaasApiError(string Code, string? Description);

internal sealed record AsaasApiDiagnostic(int? HttpStatusCode, IReadOnlyList<AsaasApiError> Errors);

internal sealed record AsaasCreatePaymentCharge(
    string CustomerId,
    decimal Amount,
    DateOnly DueDate,
    string Reference,
    string? Description);

internal sealed record AsaasPaymentCharge(
    string Id,
    string CustomerId,
    decimal Amount,
    string? Reference,
    string BillingType,
    string Status,
    bool Deleted,
    string ProviderPayloadJson,
    string? InvoiceUrl = null);

internal sealed record AsaasBoletoBillingInfo(
    string IdentificationField,
    string? Barcode,
    string? BankSlipNumber,
    string? BankSlipUrl,
    string ProviderPayloadJson);

internal sealed record AsaasPaymentPage<T>(IReadOnlyList<T> Items, bool HasMore, int Offset, int Limit);
