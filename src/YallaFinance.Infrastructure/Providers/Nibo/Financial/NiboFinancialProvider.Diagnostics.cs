using Refit;
using System.Text.Json;
using YallaFinance.Application.Services.Financial.Contracts;


namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    private static async Task<(string? Code, string? Message)> ReadProviderErrorAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var code = ReadJsonStringValue(root, "error");
            var message = ReadJsonStringValue(root, "error_description")
                ?? ReadJsonStringValue(root, "message")
                ?? ReadJsonStringValue(root, "title");
            if (message is null && root.TryGetProperty("exception", out var exception)
                && exception.ValueKind == JsonValueKind.Object)
                message = ReadJsonStringValue(exception, "Message");
            return (LimitDiagnosticValue(code, 100), LimitDiagnosticValue(message, 500));
        }
        catch (Exception exception) when (exception is JsonException or IOException or NotSupportedException)
        {
            return (null, null);
        }
    }

    private static string? ReadJsonStringValue(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? LimitDiagnosticValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static FinancialProviderDiagnostic CreateFinancialProviderDiagnostic(
        string operation,
        string method,
        string endpoint,
        int? status = null,
        Exception? exception = null)
    {
        // Refit wraps transport failures. Retain only technical classifications, never request data or messages.
        if (exception is ApiRequestException { InnerException: { } transport })
            exception = transport;
        status ??= exception switch
        {
            ApiException niboApi => (int)niboApi.StatusCode,
            HttpRequestException { StatusCode: { } httpStatus } => (int)httpStatus,
            _ => null
        };
        return new(operation, method, endpoint, status, exception?.GetType().Name,
            (exception as HttpRequestException)?.HttpRequestError.ToString());
    }
}
