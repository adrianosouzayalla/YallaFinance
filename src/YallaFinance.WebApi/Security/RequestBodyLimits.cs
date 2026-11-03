namespace YallaFinance.WebApi.Security;

public static class RequestBodyLimits
{
    /// <summary>
    /// Maximum request body accepted by the API. Batch inputs are capped at 100 items and text fields at 2000 characters,
    /// and there are no file uploads, so 1 MiB is well above legitimate payloads.
    /// </summary>
    public const long MaxBodyBytes = 1024 * 1024;
}
