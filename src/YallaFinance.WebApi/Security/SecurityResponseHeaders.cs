using Microsoft.AspNetCore.Http;

namespace YallaFinance.WebApi.Security;

public static class SecurityResponseHeaders
{
    public const string StrictTransportSecurityValue = "max-age=31536000";

    /// <summary>Adds the API security headers. HSTS is sent only in production, so development over HTTP is not affected.</summary>
    public static void AddToResponse(IHeaderDictionary headers, bool isProduction)
    {
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        if (isProduction)
            headers["Strict-Transport-Security"] = StrictTransportSecurityValue;
    }
}
