using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Application.Repositories.Connections;
using System.Globalization;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Sicoob;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal sealed class SicoobConfigurationReader(ICompanyConnectionRepository connections)
{
    private static readonly HashSet<string> AllowedSettings = new(StringComparer.Ordinal)
    {
        "Connection.Environment", "Connection.BaseUrl", "Authentication.ClientId",
        "Authentication.TokenUrl", "Authentication.Scopes", "Authentication.CertificatePem",
        "Authentication.PrivateKeyPem", "Authentication.PrivateKeyPassword",
        "Pix.ReceiverKey", "Pix.ReceivedLookbackDays", "Transport.RequestTimeoutSeconds"
    };

    public async Task<SicoobOptions> ReadAsync(
        ProviderConnection connection,
        bool creating,
        CancellationToken cancellationToken)
    {
        var settings = await connections.ListRuntimeSettingsAsync(
            connection.TenantId,
            connection.Id,
            cancellationToken);
        if (settings.Any(setting => setting.TenantId != connection.TenantId || setting.ConnectionId != connection.Id))
            throw new IntegrationConfigurationException("SettingIdentityMismatch");
        if (settings.Any(setting => !AllowedSettings.Contains(setting.Group + "." + setting.Key)))
            throw new IntegrationConfigurationException("UnknownProviderSetting");
        if (settings.GroupBy(setting => (setting.Group, setting.Key)).Any(settingGroup => settingGroup.Count() != 1))
            throw new IntegrationConfigurationException("DuplicateProviderSetting");
        var values = settings.ToDictionary(
            setting => setting.Group + "." + setting.Key,
            setting => setting.Value,
            StringComparer.Ordinal);
        string? Optional(string key) => values.GetValueOrDefault(key);
        string Required(string key) => !string.IsNullOrWhiteSpace(Optional(key)) ? Optional(key)! :
            throw new IntegrationConfigurationException("SettingMissing");

        var environment = Required("Connection.Environment");
        if (environment != "Production")
            throw new IntegrationConfigurationException("UnsupportedEnvironment");
        var baseUrl = ParseAndValidateProviderUri(
            Required("Connection.BaseUrl"),
            "api.sicoob.com.br",
            "/pix/api/v2",
            "InvalidBaseUrl");
        var clientId = Required("Authentication.ClientId");
        SicoobAuthentication.ValidateHeader(clientId);
        if (!int.TryParse(
            Required("Transport.RequestTimeoutSeconds"),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var timeout)
            || timeout is < 1 or > 60)
            throw new IntegrationConfigurationException("InvalidTimeout");
        var receiverKey = creating ? Required("Pix.ReceiverKey") : Optional("Pix.ReceiverKey");
        if (receiverKey is not null &&
            (receiverKey.Length > 77 ||
                receiverKey.Any(char.IsControl) ||
                receiverKey.Any(char.IsWhiteSpace)))
            throw new IntegrationConfigurationException("InvalidReceiverKey");

        var tokenUrl = ParseAndValidateProviderUri(Required("Authentication.TokenUrl"), "auth.sicoob.com.br",
            "/auth/realms/cooperado/protocol/openid-connect/token", "InvalidTokenUrl");
        var scopes = Required("Authentication.Scopes");
        var permissions = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (scopes.Any(char.IsControl) ||
            !permissions.Contains("cob.read") ||
            (creating &&
                !permissions.Contains("cob.write")))
            throw new IntegrationConfigurationException("InvalidScopes");
        var certificatePem = Required("Authentication.CertificatePem");
        if (!certificatePem.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
            throw new IntegrationConfigurationException("InvalidClientCertificate");
        var privateKeyPem = Required("Authentication.PrivateKeyPem");
        if (!privateKeyPem.Contains("PRIVATE KEY-----", StringComparison.Ordinal))
            throw new IntegrationConfigurationException("InvalidClientCertificate");

        return new SicoobOptions
        {
            BaseUrl = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/"),
            ClientId = clientId,
            Timeout = TimeSpan.FromSeconds(timeout),
            ReceiverKey = receiverKey,
            TokenUrl = tokenUrl,
            Scopes = scopes,
            CertificatePem = certificatePem,
            PrivateKeyPem = privateKeyPem,
            PrivateKeyPassword = Optional("Authentication.PrivateKeyPassword"),
            ReceivedLookbackDays = Optional("Pix.ReceivedLookbackDays")
        };
    }

    private static Uri ParseAndValidateProviderUri(string address, string allowedHost, string allowedPath, string code)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || !string.Equals(uri.Host, allowedHost, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.AbsolutePath.TrimEnd('/'), allowedPath, StringComparison.Ordinal))
            throw new IntegrationConfigurationException(code);
        return uri;
    }

}
