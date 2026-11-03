using Refit;
using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal interface ISicoobPixApi
{
    [Put("/cob/{txid}")]
    [Headers("Content-Type: application/json")]
    Task<HttpResponseMessage> CreateAsync(
        string txid,
        [Body(buffered: true)] SicoobCreatePixChargeRequest request,
        [Header("client_id")] string clientId,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken);
    [Get("/cob/{txid}")]
    Task<HttpResponseMessage> GetAsync(
        string txid,
        [Header("client_id")] string clientId,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken);

    [Get("/pix")]
    Task<HttpResponseMessage> GetReceivedAsync(
        [Query] SicoobPixReceivedQueryParameters query,
        [Header("client_id")] string clientId,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken);
}
