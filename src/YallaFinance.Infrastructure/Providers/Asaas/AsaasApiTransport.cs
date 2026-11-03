using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Asaas.Payments;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Asaas;

internal sealed class AsaasApiTransport(
    AsaasConfigurationReader configuration,
    AsaasClient client,
    ILogger logger)
{
    public async Task<AsaasApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string? path,
        object? body,
        ProviderConnection connection,
        Func<JsonElement, T> read,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? sensitiveValues = null,
        bool acceptJson = false,
        bool sendEmptyJsonBody = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (path is null)
            return new(AsaasApiOutcome.Rejected, default, "Validation");

        var dispatched = false;
        int? httpStatus = null;
        var started = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var options = await configuration.ReadAsync(connection, cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.Timeout);
            cancellationToken.ThrowIfCancellationRequested();
            dispatched = true;
            using var response = await client.SendAsync(method, path, body, options, timeout.Token,
                acceptJson, sendEmptyJsonBody);
            httpStatus = (int)response.StatusCode;
            var diagnostic = response.IsSuccessStatusCode
                ? null
                : await ReadProviderErrorsAsync(response, options.ApiToken, sensitiveValues, timeout.Token);
            if (httpStatus == 400)
            {
                var codes = diagnostic!.Errors.Select(error => error.Code).ToArray();
                var errorCode = codes.Length == 1 && codes[0] == "invalid_creditCard" ? "CardDeclined"
                    : codes.Length > 0 && codes.All(code => code is "invalid_value" or "invalid_customer"
                        or "invalid_cpfCnpj" or "invalid_dueDate" or "invalid_email" or "invalid_postalCode")
                        ? "Validation"
                        : "ProviderRejected";
                return new(AsaasApiOutcome.Rejected, default, errorCode, diagnostic);
            }
            if (!response.IsSuccessStatusCode)
                return HttpError<T>(httpStatus.Value, diagnostic);

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
            return new(AsaasApiOutcome.Completed, read(document.RootElement), null,
                new(httpStatus, []));
        }
        catch (IntegrationConfigurationException exception)
        {
            return new(AsaasApiOutcome.ConfigurationError, default, exception.Code);
        }
        catch (OperationCanceledException) when (!dispatched && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (!dispatched && exception is DbException or System.IO.IOException
            or HttpRequestException or TimeoutException or OperationCanceledException)
        {
            return new(AsaasApiOutcome.ConfigurationError, default, "ConnectionUnavailable");
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
            or JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException
            or System.IO.IOException)
        {
            return new(AsaasApiOutcome.Indeterminate, default, "UnresolvedProviderResult",
                httpStatus is null ? null : new(httpStatus, []));
        }
        finally
        {
            logger.LogInformation("Asaas operation ended. ConnectionId {ConnectionId}; Method {Method}; Dispatched {Dispatched}; HttpStatus {HttpStatus}; DurationMs {DurationMs}",
                connection.Id, method.Method, dispatched, httpStatus, started.ElapsedMilliseconds);
        }
    }

    private static async Task<AsaasApiDiagnostic> ReadProviderErrorsAsync(
        HttpResponseMessage response,
        string apiToken,
        IReadOnlyList<string>? sensitiveValues,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Array)
                throw new JsonException();

            var providerErrors = new List<AsaasApiError>();
            foreach (var item in errors.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("code", out var code)
                    || code.ValueKind != JsonValueKind.String || !Text(code.GetString()))
                    throw new JsonException();

                var description = item.TryGetProperty("description", out var value)
                    && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                providerErrors.Add(new(Protect(code.GetString()!), description is null ? null : Protect(description)));
            }
            if (providerErrors.Count == 0 && (int)response.StatusCode == 400)
                throw new JsonException();
            return new((int)response.StatusCode, providerErrors);
        }
        catch (JsonException) when ((int)response.StatusCode != 400)
        {
            return new((int)response.StatusCode, []);
        }

        string Protect(string value)
        {
            if (!string.IsNullOrEmpty(apiToken))
                value = value.Replace(apiToken, "[REDACTED]", StringComparison.Ordinal);
            foreach (var secret in sensitiveValues ?? [])
            {
                if (string.IsNullOrEmpty(secret))
                    continue;
                value = value.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
                if (secret.Length is >= 13 and <= 19 && secret.All(char.IsAsciiDigit))
                    value = Regex.Replace(value, string.Join(@"[\s-]*", secret.Select(digit => digit.ToString())), "[REDACTED]",
                        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
            }
            return value;
        }
    }

    private static AsaasApiResult<T> HttpError<T>(int status, AsaasApiDiagnostic? diagnostic) => status switch
    {
        400 => new(AsaasApiOutcome.Rejected, default, "Validation", diagnostic),
        401 or 403 => new(AsaasApiOutcome.Rejected, default, "CredentialOrPermission", diagnostic),
        404 => new(AsaasApiOutcome.Rejected, default, "NotFound", diagnostic),
        429 => new(AsaasApiOutcome.Indeterminate, default, "RateLimited", diagnostic),
        >= 500 => new(AsaasApiOutcome.Indeterminate, default, "RemoteFailure", diagnostic),
        _ => new(AsaasApiOutcome.Indeterminate, default, "UnexpectedHttpStatus", diagnostic)
    };

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsControl);
}
