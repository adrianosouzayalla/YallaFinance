namespace YallaFinance.Infrastructure.Services.Financial;

internal static class FinancialDocument
{
    public static string? Normalize(string? document)
    {
        if (string.IsNullOrWhiteSpace(document) ||
            document.Any(c => !(c is >= '0' and <= '9' or '.' or '-' or '/' or ' ')))
            return null;
        var digits = new string(document.Where(c => c is >= '0' and <= '9').ToArray());
        return digits.Length is 11 or 14 ? digits : null;
    }
}
