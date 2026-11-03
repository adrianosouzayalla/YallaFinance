using System.Globalization;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Asaas;

internal sealed class AsaasConfigurationReader(ICompanyConnectionRepository connections)
{
    private static readonly HashSet<string> AllowedSettings = new(StringComparer.Ordinal)
    {
        "Connection.BaseUrl", "Authentication.ApiToken", "Pix.ReceivedLookbackDays",
        "Transport.RequestTimeoutSeconds"
    };

    public async Task<AsaasOptions> ReadAsync(ProviderConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Id == Guid.Empty || connection.TenantId == Guid.Empty || !connection.Enabled
            || connection.Provider != "Asaas" || connection.Group != "PaymentProvider")
            throw new IntegrationConfigurationException("InvalidConnection");
        var current = await connections.GetRuntimeAsync(connection.TenantId, connection.Id, cancellationToken);
        if (current is null || current.Id != connection.Id || current.TenantId != connection.TenantId
            || !current.Enabled || current.Provider != connection.Provider || current.Group != connection.Group)
            throw new IntegrationConfigurationException("ConnectionIdentityMismatch");
        var settings = await connections.ListRuntimeSettingsAsync(connection.TenantId, connection.Id, cancellationToken);
        if (settings.Any(setting => setting.TenantId != connection.TenantId || setting.ConnectionId != connection.Id))
            throw new IntegrationConfigurationException("SettingIdentityMismatch");
        if (settings.Any(setting => !AllowedSettings.Contains(setting.Group + "." + setting.Key)))
            throw new IntegrationConfigurationException("UnknownProviderSetting");
        if (settings.GroupBy(setting => (setting.Group, setting.Key)).Any(group => group.Count() != 1))
            throw new IntegrationConfigurationException("DuplicateProviderSetting");
        var values = settings.ToDictionary(setting => setting.Group + "." + setting.Key, setting => setting.Value, StringComparer.Ordinal);
        string Required(string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new IntegrationConfigurationException("SettingMissing");
        string? Optional(string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value : null;
        if (!Uri.TryCreate(Required("Connection.BaseUrl"), UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || !uri.IsDefaultPort || uri.Host is not ("api-sandbox.asaas.com" or "api.asaas.com")
            || uri.AbsolutePath.TrimEnd('/') != "/v3" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new IntegrationConfigurationException("InvalidBaseUrl");
        var token = Required("Authentication.ApiToken");
        if (token.Any(char.IsControl) || token.Any(char.IsWhiteSpace))
            throw new IntegrationConfigurationException("InvalidCredentialFormat");
        if (!int.TryParse(Required("Transport.RequestTimeoutSeconds"), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || seconds is < 60 or > 300)
            throw new IntegrationConfigurationException("InvalidTimeout");
        var receivedLookbackDays = Optional("Pix.ReceivedLookbackDays");
        return new AsaasOptions(new Uri(uri.AbsoluteUri.TrimEnd('/') + "/"), token, TimeSpan.FromSeconds(seconds),
            receivedLookbackDays);
    }
}

// Class deliberately avoids record-generated ToString containing the credential.
internal sealed class AsaasOptions(
    Uri baseUrl,
    string apiToken,
    TimeSpan timeout,
    string? pixReceivedLookbackDays)
{
    public Uri BaseUrl { get; } = baseUrl;
    public string ApiToken { get; } = apiToken;
    public TimeSpan Timeout { get; } = timeout;
    public string? PixReceivedLookbackDays { get; } = pixReceivedLookbackDays;
}
