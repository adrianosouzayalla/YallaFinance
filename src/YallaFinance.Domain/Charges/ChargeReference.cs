using System.Globalization;

namespace YallaFinance.Domain.Charges;

/// <summary>A human reference only; UUIDs remain the identity of charges and tenants.</summary>
public static class ChargeReference
{
    public static string Format(long tenantCode, long number)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        return string.Create(CultureInfo.InvariantCulture, $"{tenantCode}-{number}");
    }

    public static bool TryParse(string? value, out long tenantCode, out long number)
    {
        tenantCode = number = 0;
        if (value is null || value.Length is < 3 or > 39)
            return false;
        var separator = value.IndexOf('-');
        if (separator <= 0 || separator == value.Length - 1)
            return false;
        var codePart = value.AsSpan(0, separator);
        var numberPart = value.AsSpan(separator + 1);
        return codePart[0] != '0' && numberPart[0] != '0'
            && long.TryParse(codePart, NumberStyles.None, CultureInfo.InvariantCulture, out tenantCode)
            && long.TryParse(numberPart, NumberStyles.None, CultureInfo.InvariantCulture, out number)
            && tenantCode > 0 && number > 0;
    }
}
