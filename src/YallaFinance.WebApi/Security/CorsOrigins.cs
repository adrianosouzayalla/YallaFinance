using Microsoft.Extensions.Configuration;

namespace YallaFinance.WebApi.Security;

/// <summary>
/// Reads the origins allowed by CORS from the Cors:AllowedOrigins section of appsettings.json (documented exception to the
/// database rule: read at startup, before any database connection, and not a secret). Without origins, no origin is allowed.
/// </summary>
public static class CorsOrigins
{
    public const string SectionPath = "Cors:AllowedOrigins";

    public static string[] From(IConfiguration configuration) =>
        configuration.GetSection(SectionPath).Get<string[]>() ?? [];
}
