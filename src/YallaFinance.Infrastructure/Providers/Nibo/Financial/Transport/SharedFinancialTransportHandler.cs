namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Transport;

internal sealed class SharedFinancialTransportHandler(HttpMessageInvoker transport) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => transport.SendAsync(request, cancellationToken);
}
