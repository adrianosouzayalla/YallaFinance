namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed class NiboOptions(string apiToken, Uri baseUrl, string? companyId = null)
{
    public string ApiToken { get; } = apiToken;
    public Uri BaseUrl { get; } = baseUrl;
    public string? CompanyId { get; } = companyId;
}
