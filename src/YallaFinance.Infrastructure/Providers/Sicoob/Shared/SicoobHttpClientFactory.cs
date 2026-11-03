using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using YallaFinance.Infrastructure.Common.Errors;

namespace YallaFinance.Infrastructure.Providers.Sicoob;

internal sealed class SicoobHttpClientFactory(HttpMessageHandler? transport = null)
{
    public HttpClient Create(SicoobOptions options)
    {
        SicoobClientCertificate? certificate = null;
        try
        {
            certificate = LoadClientCertificate(options, transport is null);
            var handler = transport ?? new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                SslOptions = CreateClientTlsOptions(certificate.Certificate)
            };
            return new HttpClient(new SicoobTransportHandler(handler, certificate, transport is null))
            {
                BaseAddress = options.BaseUrl,
                Timeout = options.Timeout,
                MaxResponseContentBufferSize = 1024 * 1024
            };
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or ArgumentException)
        {
            certificate?.Dispose();
            throw new IntegrationConfigurationException("InvalidClientCertificate");
        }
        catch
        {
            certificate?.Dispose();
            throw;
        }
    }

    internal static SicoobClientCertificate LoadClientCertificate(SicoobOptions options)
        => LoadClientCertificate(options, useSchannelKeyStore: true);

    internal static SicoobClientCertificate LoadClientCertificate(
        SicoobOptions options,
        bool useSchannelKeyStore)
    {
        var certificate = string.IsNullOrWhiteSpace(options.PrivateKeyPassword)
            ? X509Certificate2.CreateFromPem(options.CertificatePem, options.PrivateKeyPem)
            : X509Certificate2.CreateFromEncryptedPem(
                options.CertificatePem,
                options.PrivateKeyPem,
                options.PrivateKeyPassword);
        if (!certificate.HasPrivateKey || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow
            || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow)
        {
            certificate.Dispose();
            throw new CryptographicException();
        }

        // Injected transports are test doubles and do not perform a TLS handshake.
        // Keep those tests isolated from the user's persistent Windows key store.
        if (!useSchannelKeyStore || !OperatingSystem.IsWindows())
            return new(certificate, null);

        byte[]? pkcs12 = null;
        byte[]? passwordBytes = null;
        X509Certificate2? schannelCertificate = null;
        try
        {
            passwordBytes = RandomNumberGenerator.GetBytes(32);
            var password = Convert.ToHexString(passwordBytes);
            pkcs12 = certificate.Export(X509ContentType.Pkcs12, password);
            schannelCertificate = X509CertificateLoader.LoadPkcs12(
                pkcs12,
                password,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
            var keyName = GetCngPrivateKeyName(schannelCertificate);
            certificate.Dispose();
            return new(schannelCertificate, keyName);
        }
        catch
        {
            schannelCertificate?.Dispose();
            certificate.Dispose();
            throw;
        }
        finally
        {
            if (pkcs12 is not null)
                CryptographicOperations.ZeroMemory(pkcs12);
            if (passwordBytes is not null)
                CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static string GetCngPrivateKeyName(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa is RSACng rsaCng && !rsaCng.Key.IsEphemeral)
        {
            var keyName = rsaCng.Key.UniqueName;
            if (!string.IsNullOrWhiteSpace(keyName))
                return keyName;
        }

        using var ecdsa = certificate.GetECDsaPrivateKey();
        if (ecdsa is ECDsaCng ecdsaCng && !ecdsaCng.Key.IsEphemeral)
        {
            var keyName = ecdsaCng.Key.UniqueName;
            if (!string.IsNullOrWhiteSpace(keyName))
                return keyName;
        }

        throw new CryptographicException("Client certificate key is not backed by a persisted CNG key.");
    }

    internal static System.Net.Security.SslClientAuthenticationOptions CreateClientTlsOptions(X509Certificate2 certificate)
        => new()
        {
            ClientCertificates = new X509CertificateCollection { certificate }
        };
}
