namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed class NiboHttpClientFactory(HttpMessageHandler transport)
{
    public HttpClient Create(NiboOptions options) => new(transport, disposeHandler: false)
    {
        BaseAddress = options.BaseUrl,
        Timeout = TimeSpan.FromSeconds(30)
    };
}
