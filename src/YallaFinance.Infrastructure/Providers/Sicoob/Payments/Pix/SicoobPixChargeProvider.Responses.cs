using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal sealed partial class SicoobPixChargeProvider
{
    private PixChargeSnapshot? ParsePixChargeResponse(
        string body,
        string reference,
        string? receiverKey,
        CreatePixChargeData? input,
        out string failureCode)
    {
        failureCode = "ProviderResponseInvalid";
        SicoobPixChargeResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<SicoobPixChargeResponse>(body, JsonOptions);
        }
        catch (JsonException)
        {
            failureCode = "ProviderResponseInvalidJson";
            return null;
        }
        if (response is null)
            return null;
        if (response.TxId != reference)
            return SetResponseFailure(
                "ProviderResponseReferenceMismatch",
                out failureCode);
        if (response.Calendar is null)
            return SetResponseFailure("ProviderResponseMissingCalendar", out failureCode);
        if (response.Amount?.Original is not { } original || !AmountPattern().IsMatch(original)
            || !decimal.TryParse(
                original,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount) || amount <= 0)
            return SetResponseFailure("ProviderResponseInvalidAmount", out failureCode);
        if (response.Calendar.ExpirationSeconds is not { } expiration || expiration <= 0)
            return SetResponseFailure("ProviderResponseInvalidExpiration", out failureCode);
        if (response.Calendar.CreatedAt is not { } rawDate || !OffsetPattern().IsMatch(rawDate)
            || !DateTimeOffset.TryParse(
                rawDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var created))
            return SetResponseFailure("ProviderResponseInvalidCreatedAt", out failureCode);
        if (receiverKey is not null && response.ReceiverKey != receiverKey)
            return SetResponseFailure(
                "ProviderResponseReceiverMismatch",
                out failureCode);
        if (input is not null && amount != input.Amount)
            return SetResponseFailure(
                "ProviderResponseAmountMismatch",
                out failureCode);
        if (input is not null && expiration != input.ExpirationSeconds)
            return SetResponseFailure(
                "ProviderResponseExpirationMismatch",
                out failureCode);
        PixChargeStatus? status = response.Status switch
        {
            "ATIVA" => PixChargeStatus.Active,
            "CONCLUIDA" => PixChargeStatus.Completed,
            "REMOVIDA_PELO_USUARIO_RECEBEDOR" => PixChargeStatus.RemovedByReceiver,
            "REMOVIDA_PELO_PSP" => PixChargeStatus.RemovedByProvider,
            _ => null
        };
        if (status is null)
            return SetResponseFailure("ProviderResponseUnsupportedStatus", out failureCode);
        PixChargePayer? payer = null;
        if (response.Payer is { } observedPayer)
        {
            if ((observedPayer.Cpf is null) == (observedPayer.Cnpj is null) || observedPayer.Name is null)
                return SetResponseFailure("ProviderResponseInvalidPayer", out failureCode);
            payer = new(observedPayer.Cpf ?? observedPayer.Cnpj!, observedPayer.Name);
            if (!SicoobPixValidation.IsValidPixPayer(payer))
                return SetResponseFailure(
                    "ProviderResponseInvalidPayer",
                    out failureCode);
        }
        // Compara somente o que o provider informa (CPF/CNPJ e nome); CustomerId e um identificador interno.
        if (input is not null && ((input.Payer is not null
            && (payer is null || input.Payer.TaxId != payer.TaxId || input.Payer.Name != payer.Name))
            || (input.Description is not null && input.Description != response.Description)))
            return SetResponseFailure(
                "ProviderResponseRequestMismatch",
                out failureCode);
        if (response.CopyPaste is not null &&
            response.Brcode is not null &&
            response.CopyPaste != response.Brcode)
            return SetResponseFailure("ProviderResponseConflictingPixCodes", out failureCode);
        if (response.Location is not null && response.LocationData?.Location is not null
            && response.Location != response.LocationData.Location)
            return SetResponseFailure(
                "ProviderResponseConflictingLocations",
                out failureCode);
        DateTimeOffset expires;
        try
        {
            expires = created.AddSeconds(expiration);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SetResponseFailure(
                "ProviderResponseExpirationOverflow",
                out failureCode);
        }
        var copyPaste = response.CopyPaste ?? response.Brcode;
        var pixCodeSource = response.CopyPaste is not null
            ? "PixCopiaECola"
            : response.Brcode is not null
                ? "Brcode"
                : "Missing";
        var isValidPixCode = SicoobPixCode.IsValid(
            copyPaste,
            amount,
            response.Location ?? response.LocationData?.Location,
            out var pixCodeValidationCode);
        var instrument = isValidPixCode ? copyPaste : null;
        failureCode = isValidPixCode
            ? string.Empty
            : $"PixCode_{pixCodeSource}_{pixCodeValidationCode}";
        return new(
            reference,
            amount,
            created.ToUniversalTime(),
            expires.ToUniversalTime(),
            status.Value,
            instrument,
            clock.GetUtcNow(),
            payer,
            response.Description,
            response.Location ?? response.LocationData?.Location,
            ReadLocationId(response.LocationData?.Id));
    }

    private static PixChargeSnapshot? SetResponseFailure(string code, out string failureCode)
    {
        failureCode = code;
        return null;
    }

    private static string? ReadProblemType(string body)
    {
        const string expectedPrefix = "https://pix.bcb.gov.br/api/v2/error/";
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            // The gateway uses a different error envelope from the Pix API. Recognize only this known message.
            if (document.RootElement.TryGetProperty("moreInformation", out var information)
                && information.ValueKind == JsonValueKind.String
                && information.GetString() == "Invalid request type")
                return "InvalidRequestType";
            if (!document.RootElement.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String)
                return null;

            var value = type.GetString();
            if (value is null || !value.StartsWith(expectedPrefix, StringComparison.Ordinal))
                return null;
            var name = value[expectedPrefix.Length..];
            return name.Length is > 0 and <= 64
                && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
                    ? name
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadLocationId(JsonElement? id) => id is { } value ? value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    } : null;

    private static bool IsKnownProblem(HttpResponseMessage response, string body, int status, string name)
    {
        if ((int)response.StatusCode != status)
            return false;
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String
                && type.GetString() == "https://pix.bcb.gov.br/api/v2/error/" + name
                && document.RootElement.TryGetProperty("status", out var value) && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var actual) && actual == status;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[0-9]{1,10}\.[0-9]{2}\z", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"T.*(?:Z|[+-][0-9]{2}:[0-9]{2})\z", RegexOptions.CultureInvariant)]
    private static partial Regex OffsetPattern();
}
