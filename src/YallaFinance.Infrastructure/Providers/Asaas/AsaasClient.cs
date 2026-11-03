using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Asaas;

internal sealed class AsaasClient : IDisposable
{
    private readonly HttpClient client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AsaasClient() : this(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectTimeout = TimeSpan.FromSeconds(15)
    }, true) { }

    internal AsaasClient(HttpMessageHandler transport) : this(transport, false) { }

    private AsaasClient(HttpMessageHandler transport, bool ownsTransport)
    {
        client = new HttpClient(transport, ownsTransport) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, AsaasOptions options,
        CancellationToken cancellationToken, bool acceptJson = false, bool sendEmptyJsonBody = false)
    {
        using var request = new HttpRequestMessage(method, new Uri(options.BaseUrl, path));
        request.Headers.Add("access_token", options.ApiToken);
        request.Headers.UserAgent.ParseAdd("YallaFinance/" + (typeof(AsaasClient).Assembly.GetName().Version?.ToString() ?? "1.0"));
        if (acceptJson)
            request.Headers.Accept.ParseAdd("application/json");
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        else if (sendEmptyJsonBody)
        {
            request.Content = new ByteArrayContent([]);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }
        // One send only. No evidence handler, retries, hedging or HttpClientFactory body logging.
        return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }

    public void Dispose() => client.Dispose();
}
