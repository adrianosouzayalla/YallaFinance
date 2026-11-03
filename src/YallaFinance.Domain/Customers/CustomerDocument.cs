namespace YallaFinance.Domain.Customers;

public static class CustomerDocument
{
    // Preserve significant ASCII letters and leading zeros. Mask characters are presentation only.
    public static string Normalize(string? document) => document is null ? "" :
        string.Concat(document.Where(char.IsAsciiLetterOrDigit)).ToUpperInvariant();

    public static bool HasValidFormat(string? document, string? documentType)
    {
        if (string.IsNullOrWhiteSpace(document)
            || document.Any(character => !char.IsAsciiLetterOrDigit(character) && !"./ -".Contains(character)))
            return false;
        var canonical = Normalize(document);
        return documentType switch
        {
            "CPF" => canonical.Length == 11 && canonical.All(char.IsAsciiDigit),
            "CNPJ" => canonical.Length == 14 && canonical[..12].All(char.IsAsciiLetterOrDigit)
                && canonical[12..].All(char.IsAsciiDigit),
            _ => false
        };
    }
}
