using Serilog.AspNetCore;
using Serilog.Events;

namespace YallaFinance.WebApi.Payments;

public static class PaymentRequestLogPath
{
    private const string PaymentPrefix = "/api/payments/";
    private static readonly HashSet<string> FixedSingleSegmentRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "charges",
        "prepare",
    };

    public static string Redact(string? path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(PaymentPrefix, StringComparison.OrdinalIgnoreCase))
            return path ?? "/";

        var suffix = path[PaymentPrefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (suffix.Length == 1)
            return suffix[0].Length == 0 || FixedSingleSegmentRoutes.Contains(suffix[0])
                ? path : "/api/payments/{token}";
        if (suffix.Length == 2 && string.Equals(suffix[1], "attempts", StringComparison.OrdinalIgnoreCase))
            return "/api/payments/{token}/attempts";
        return path;
    }

    public static void ConfigureRequestLogging(RequestLoggingOptions options) =>
        options.GetMessageTemplateProperties = (context, path, elapsedMs, statusCode) =>
            [
            new LogEventProperty("RequestMethod", new ScalarValue(context.Request.Method)),
            new LogEventProperty("RequestPath", new ScalarValue(Redact(path))),
            new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
            new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
        ];
}
