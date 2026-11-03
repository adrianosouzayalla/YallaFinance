using System.Text.Json;
using YallaFinance.Infrastructure.Common.Errors;

namespace YallaFinance.Infrastructure.Providers.Sicoob;

internal sealed class SicoobAuthentication
{
    public async Task<string> GetAccessTokenAsync(
        HttpClient client,
        SicoobOptions options,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["scope"] = options.Scopes
        };
        using var content = new FormUrlEncodedContent(values);
        using var response = await client.PostAsync(options.TokenUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new IntegrationConfigurationException($"AuthenticationRejected_HttpStatus_{(int)response.StatusCode}");
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("access_token", out var token)
                || token.ValueKind != JsonValueKind.String || !root.TryGetProperty("token_type", out var type)
                || type.ValueKind != JsonValueKind.String || !string.Equals(
                    type.GetString(),
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase)
                || !root.TryGetProperty("expires_in", out var expires) || !expires.TryGetInt32(out var seconds) || seconds <= 0)
                throw new IntegrationConfigurationException("InvalidAuthenticationResponse");
            ValidateHeader(token.GetString()!);
            return token.GetString()!;
        }
        catch (JsonException)
        {
            throw new IntegrationConfigurationException("InvalidAuthenticationResponse");
        }
        catch (InvalidOperationException)
        {
            throw new IntegrationConfigurationException("InvalidAuthenticationResponse");
        }
    }

    internal static void ValidateHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(character => character <= 32 || character >= 127))
            throw new IntegrationConfigurationException("InvalidCredential");
    }
}
