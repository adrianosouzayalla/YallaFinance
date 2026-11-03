using System.Globalization;
using System.Text.Json;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Asaas.Payments.Pix;

internal sealed class AsaasPixOperations(AsaasApiTransport transport)
{
    public Task<AsaasApiResult<AsaasPaymentPixQrCode>> GetPaymentQrCodeAsync(
        string paymentId,
        ProviderConnection connection,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, Identifier(paymentId)
            ? $"payments/{Uri.EscapeDataString(paymentId)}/pixQrCode" : null,
            null, connection, ReadPaymentQrCode, cancellationToken, acceptJson: true,
            sendEmptyJsonBody: true);

    public Task<AsaasApiResult<AsaasPaymentPage<AsaasPixReceiptPayment>>> FindReceivedPaymentsAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        int offset,
        int limit,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (offset < 0 || limit is < 1 or > 100 || start > end)
            return Invalid<AsaasPaymentPage<AsaasPixReceiptPayment>>(cancellationToken);
        var path = "payments?billingType=PIX&status=RECEIVED"
            + "&paymentDate%5Bge%5D=" + Uri.EscapeDataString(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            + "&paymentDate%5Ble%5D=" + Uri.EscapeDataString(end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            + "&offset=" + offset.ToString(CultureInfo.InvariantCulture)
            + "&limit=" + limit.ToString(CultureInfo.InvariantCulture);
        return SendAsync(HttpMethod.Get, path, null, connection,
            json => ReadPage(json, offset, limit), cancellationToken);
    }

    public Task<AsaasApiResult<AsaasPixTransaction>> GetPixTransactionAsync(
        string transactionId,
        ProviderConnection connection,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, Identifier(transactionId) ? $"pix/transactions/{Uri.EscapeDataString(transactionId)}" : null,
            null, connection, json => ReadPixTransaction(json, transactionId), cancellationToken);

    private Task<AsaasApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string? path,
        object? body,
        ProviderConnection connection,
        Func<JsonElement, T> read,
        CancellationToken cancellationToken,
        bool acceptJson = false,
        bool sendEmptyJsonBody = false) =>
        transport.SendAsync(method, path, body, connection, read, cancellationToken,
            acceptJson: acceptJson, sendEmptyJsonBody: sendEmptyJsonBody);

    private static AsaasPaymentPixQrCode ReadPaymentQrCode(JsonElement json)
    {
        var payload = RequiredText(json, "payload", 4096);
        var expiresAt = ReadDateTimeOffset(json, "expirationDate");
        var encodedImage = OptionalText(json, "encodedImage");
        if (encodedImage is { Length: > 1_000_000 })
            throw new JsonException();
        return new(payload, expiresAt, encodedImage);
    }

    private static AsaasPaymentPage<AsaasPixReceiptPayment> ReadPage(JsonElement json, int offset, int limit)
    {
        var data = json.GetProperty("data");
        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() > limit)
            throw new JsonException();
        var items = data.EnumerateArray().Select(ReadReceiptPayment).ToArray();
        return new(items, json.GetProperty("hasMore").GetBoolean(), offset, limit);
    }

    private static AsaasPixReceiptPayment ReadReceiptPayment(JsonElement json)
    {
        var id = RequiredText(json, "id", 100);
        var billingType = RequiredText(json, "billingType", 40);
        var status = RequiredText(json, "status", 80);
        var amount = json.GetProperty("value").GetDecimal();
        var reference = OptionalText(json, "externalReference");
        var paymentDate = ReadDateTimeOffset(json, "paymentDate");
        var transactionId = OptionalIdentifier(json, "pixTransaction");
        if (!string.Equals(billingType, "PIX", StringComparison.Ordinal)
            || !string.Equals(status, "RECEIVED", StringComparison.Ordinal)
            || amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new JsonException();
        return new(id, amount, reference, paymentDate, transactionId,
            AsaasPaymentOperations.SanitizeProviderPayload(json));
    }

    private static AsaasPixTransaction ReadPixTransaction(JsonElement json, string expectedId)
    {
        var id = RequiredText(json, "id", 100);
        var endToEndId = RequiredText(json, "endToEndIdentifier", 100);
        var type = RequiredText(json, "type", 50);
        var amount = json.GetProperty("value").GetDecimal();
        var effectiveDate = ReadDateTimeOffset(json, "effectiveDate");
        var paymentId = OptionalText(json, "payment");
        if (!string.Equals(id, expectedId, StringComparison.Ordinal)
            || !string.Equals(type, "CREDIT", StringComparison.Ordinal)
            || amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new JsonException();
        return new(endToEndId, amount, effectiveDate, paymentId,
            AsaasPaymentOperations.SanitizeProviderPayload(json));
    }

    private static DateTimeOffset ReadDateTimeOffset(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String
            || !DateTime.TryParse(property.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var localOrUtc))
            throw new JsonException();
        var raw = property.GetString()!;
        var hasExplicitOffset = raw.EndsWith('Z')
            || raw.Length >= 6 && raw[^6] is '+' or '-' && raw[^3] == ':';
        if (hasExplicitOffset && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var value))
            return value.ToUniversalTime();
        var saoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        return new DateTimeOffset(localOrUtc, saoPaulo.GetUtcOffset(localOrUtc)).ToUniversalTime();
    }

    private static string RequiredText(JsonElement json, string propertyName, int maxLength)
    {
        var value = OptionalText(json, propertyName);
        return value is { Length: > 0 } && value.Length <= maxLength && !value.Any(char.IsControl)
            ? value : throw new JsonException();
    }

    private static string? OptionalText(JsonElement json, string propertyName) =>
        json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() : null;

    private static string? OptionalIdentifier(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property))
            return null;
        if (property.ValueKind == JsonValueKind.String)
            return property.GetString();
        return property.ValueKind == JsonValueKind.Object && property.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
    }

    private static bool Identifier(string value) => value is { Length: > 0 and <= 100 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private static Task<AsaasApiResult<T>> Invalid<T>(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AsaasApiResult<T>(AsaasApiOutcome.Rejected, default, "Validation"));
    }

}

internal sealed record AsaasPaymentPixQrCode(string Payload, DateTimeOffset ExpiresAt, string? EncodedImage);

internal sealed record AsaasPixReceiptPayment(
    string Id,
    decimal Amount,
    string? Reference,
    DateTimeOffset ReceivedAt,
    string? PixTransactionId,
    string ProviderPayloadJson);

internal sealed record AsaasPixTransaction(
    string EndToEndId,
    decimal Amount,
    DateTimeOffset ReceivedAt,
    string? PaymentId,
    string ProviderPayloadJson);
