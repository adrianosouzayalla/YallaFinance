using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Repositories.Connections;
namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed class NiboConfigurationReader(ICompanyConnectionRepository connections)
{
    public async Task<NiboOptions> ReadAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var settings = await connections.ListRuntimeSettingsAsync(
            connection.TenantId,
            connection.Id,
            cancellationToken);
        if (settings.Any(setting => !(setting.Group == "Authentication" && setting.Key == "ApiToken")
            && !(setting.Group == "Connection" && setting.Key == "BaseUrl")
            && !(setting.Group == "Connection" && setting.Key == "CompanyId")))
            throw new IntegrationConfigurationException("UnknownProviderSetting");
        var apiToken = settings.SingleOrDefault(setting =>
            setting.Group == "Authentication" && setting.Key == "ApiToken")?.Value;
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new IntegrationConfigurationException("CredentialMissing");
        if (apiToken.Any(char.IsControl))
            throw new IntegrationConfigurationException("InvalidCredentialFormat");
        var configuredBaseUrl = settings.SingleOrDefault(setting =>
            setting.Group == "Connection" && setting.Key == "BaseUrl")?.Value;
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
            throw new IntegrationConfigurationException("BaseUrlMissing");
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUrl)
            || baseUrl.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(baseUrl.UserInfo)
            || !string.IsNullOrEmpty(baseUrl.Query) || !string.IsNullOrEmpty(baseUrl.Fragment))
            throw new IntegrationConfigurationException("InvalidBaseUrl");
        var companyId = settings.SingleOrDefault(setting =>
            setting.Group == "Connection" && setting.Key == "CompanyId")?.Value;
        if (companyId is not null && (string.IsNullOrWhiteSpace(companyId) || companyId.Any(char.IsControl)))
            throw new IntegrationConfigurationException("InvalidCompanyId");
        return new NiboOptions(apiToken, new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/"), companyId);
    }
}
