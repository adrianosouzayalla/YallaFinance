namespace YallaFinance.Infrastructure.Providers.Sicoob;

// Owns the per-call certificate and real transport. Injected transports remain owned by the caller.
internal sealed class SicoobTransportHandler(
    HttpMessageHandler transport,
    SicoobClientCertificate? certificate,
    bool ownsTransport) : HttpMessageHandler
{
    private readonly HttpMessageInvoker invoker = new(transport, ownsTransport);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => invoker.SendAsync(request, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                invoker.Dispose();
            }
            finally
            {
                try
                {
                    certificate?.Dispose();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
            return;
        }
        base.Dispose(disposing);
    }
}
