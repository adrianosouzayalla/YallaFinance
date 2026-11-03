using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace YallaFinance.Infrastructure.Providers.Sicoob;

internal sealed class SicoobClientCertificate(X509Certificate2 certificate, string? persistedKeyName) : IDisposable
{
    private bool disposed;

    public X509Certificate2 Certificate { get; } = certificate;

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        try
        {
            Certificate.Dispose();
        }
        finally
        {
            if (persistedKeyName is not null && OperatingSystem.IsWindows())
            {
                using var key = CngKey.Open(
                    persistedKeyName,
                    CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    CngKeyOpenOptions.UserKey);
                key.Delete();
            }
        }
    }
}
