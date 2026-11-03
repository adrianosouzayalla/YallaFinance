namespace YallaFinance.Infrastructure.Repositories.Access;

internal static class TenantAccessEmail
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
