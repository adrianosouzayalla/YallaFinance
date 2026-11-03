namespace YallaFinance.Infrastructure.Providers.Sicoob;

// Deliberately not a record: generated ToString must never expose credentials.
internal sealed class SicoobOptions
{
    public required Uri BaseUrl
    {
        get; init;
    }
    public required string ClientId
    {
        get; init;
    }
    public required TimeSpan Timeout
    {
        get; init;
    }
    public string? ReceiverKey
    {
        get; init;
    }
    public required Uri TokenUrl
    {
        get; init;
    }
    public required string Scopes
    {
        get; init;
    }
    public required string CertificatePem
    {
        get; init;
    }
    public required string PrivateKeyPem
    {
        get; init;
    }
    public string? PrivateKeyPassword
    {
        get; init;
    }
    public string? ReceivedLookbackDays
    {
        get; init;
    }

    public override string ToString() => "SicoobOptions [redacted]";
}
