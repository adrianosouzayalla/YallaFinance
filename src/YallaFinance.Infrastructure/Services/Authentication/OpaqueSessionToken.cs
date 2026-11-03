using System.Security.Cryptography;
namespace YallaFinance.Infrastructure.Services.Authentication;

public static class OpaqueSessionToken
{
    public static (string Token, Guid Id) Create()
    {
        while (true)
        {
            var token = EncodeBase64UrlToken(RandomNumberGenerator.GetBytes(32));
            if (TryGetId(token, out var id)) return (token, id);
        }
    }
    public static bool TryGetId(string? token, out Guid id)
    {
        id = Guid.Empty;
        if (token is null || token.Length != 43 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) return false;
        try
        {
            var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/') + "=");
            if (bytes.Length != 32 || EncodeBase64UrlToken(bytes) != token) return false;
            // Existing UUID primary key stores a 128-bit SHA-256 fingerprint.
            // The 256-bit random bearer is never persisted; knowing a session ID
            // (including a legacy JWT's sid) cannot reconstruct a usable bearer.
            id = new Guid(SHA256.HashData(bytes).AsSpan(0, 16));
            return id != Guid.Empty;
        }
        catch (FormatException) { return false; }
    }
    private static string EncodeBase64UrlToken(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
