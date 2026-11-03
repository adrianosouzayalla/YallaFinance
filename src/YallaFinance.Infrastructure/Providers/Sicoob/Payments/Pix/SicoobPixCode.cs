using System.Globalization;
using System.Text;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal static class SicoobPixCode
{
    // Validate the received BR Code; never generate a substitute from location.
    public static bool IsValid(string? code, decimal amount, string? location, out string validationFailureCode)
    {
        validationFailureCode = "Missing";
        if (string.IsNullOrEmpty(code))
            return false;
        if (code.Length > 1024)
            return SetFailure("TooLong", out validationFailureCode);
        if (code.Any(char.IsControl))
            return SetFailure("ControlCharacter", out validationFailureCode);
        var bytes = Encoding.UTF8.GetBytes(code);
        if (bytes.Length < 12)
            return SetFailure("TooShort", out validationFailureCode);
        if (Encoding.ASCII.GetString(bytes.AsSpan(bytes.Length - 8, 4)) != "6304")
            return SetFailure("CrcFieldMissing", out validationFailureCode);
        if (!ushort.TryParse(
            code.AsSpan(code.Length - 4),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out var expectedCrc))
            return SetFailure("CrcValueInvalid", out validationFailureCode);
        ushort crc = 0xffff;
        foreach (var value in bytes.AsSpan(0, bytes.Length - 4))
        {
            crc ^= (ushort)(value << 8);
            for (var bitIndex = 0; bitIndex < 8; bitIndex++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        if (crc != expectedCrc)
            return SetFailure("CrcMismatch", out validationFailureCode);
        if (!ParsePixCodeFields(bytes, out var pixCodeFields))
            return SetFailure("MalformedTlv", out validationFailureCode);
        if (pixCodeFields.GetValueOrDefault("00") != "01" || pixCodeFields.GetValueOrDefault("01") != "12"
            || pixCodeFields.GetValueOrDefault("53") != "986" || pixCodeFields.GetValueOrDefault("58") != "BR"
            || string.IsNullOrWhiteSpace(pixCodeFields.GetValueOrDefault("59")) || string.IsNullOrWhiteSpace(pixCodeFields.GetValueOrDefault("60")))
            return SetFailure("PayloadFieldsInvalid", out validationFailureCode);
        if (pixCodeFields.TryGetValue("54", out var rawAmount)
            && (!decimal.TryParse(
                rawAmount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var qrAmount) || qrAmount != amount))
            return SetFailure("AmountInvalidOrMismatched", out validationFailureCode);
        var hasMatchingLocation = pixCodeFields.Where(merchantAccountField =>
                int.TryParse(merchantAccountField.Key, out var fieldId) && fieldId is >= 26 and <= 51)
            .Any(merchantAccountField => ParsePixCodeFields(
                Encoding.UTF8.GetBytes(merchantAccountField.Value), out var merchantAccount)
                && string.Equals(
                    merchantAccount.GetValueOrDefault("00"),
                    "br.gov.bcb.pix",
                    StringComparison.OrdinalIgnoreCase)
                && merchantAccount.GetValueOrDefault("25") is { Length: > 0 } merchantAddress &&
                IsValidPixLocationAddress(merchantAddress)
                && (location is null || merchantAddress == location || "https://" + merchantAddress == location));
        if (!hasMatchingLocation)
            return SetFailure("MerchantAccountOrLocationInvalid", out validationFailureCode);
        validationFailureCode = string.Empty;
        return true;
    }

    private static bool SetFailure(string failureCode, out string validationFailureCode)
    {
        validationFailureCode = failureCode;
        return false;
    }

    private static bool IsValidPixLocationAddress(string address) => !address.Any(char.IsWhiteSpace)
        && !address.Contains("://", StringComparison.Ordinal)
        && Uri.TryCreate("https://" + address, UriKind.Absolute, out var uri)
        && uri.HostNameType == UriHostNameType.Dns && uri.Host.Contains('.')
        && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 && uri.IsDefaultPort;

    private static bool ParsePixCodeFields(byte[] bytes, out Dictionary<string, string> fields)
    {
        fields = new(StringComparer.Ordinal);
        var offset = 0;
        while (offset < bytes.Length)
        {
            if (offset + 4 > bytes.Length)
                return false;
            if (bytes.AsSpan(offset, 4).ContainsAnyExceptInRange((byte)'0', (byte)'9'))
                return false;
            var fieldId = Encoding.ASCII.GetString(bytes, offset, 2);
            var length = (bytes[offset + 2] - '0') * 10 + bytes[offset + 3] - '0';
            offset += 4;
            if (offset + length > bytes.Length ||
                !fields.TryAdd(
                    fieldId,
                    Encoding.UTF8.GetString(
                        bytes,
                        offset,
                        length)))
                return false;
            offset += length;
        }
        return true;
    }
}
