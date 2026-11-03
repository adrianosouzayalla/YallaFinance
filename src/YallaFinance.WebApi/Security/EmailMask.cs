namespace YallaFinance.WebApi.Security;

/// <summary>Masks an e-mail for logs, keeping only the first letter of the local part and the domain.</summary>
public static class EmailMask
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "(vazio)";

        var value = email.Trim();
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex == value.Length - 1)
            return "***";

        return $"{value[0]}***{value[atIndex..]}";
    }
}
