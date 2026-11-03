using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Transport;
using YallaFinance.Infrastructure.Providers.Nibo.Financial;

namespace YallaFinance.Infrastructure.Providers.Nibo;

internal sealed class NiboProviderModule : IProviderModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new HttpMessageInvoker(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        }));

        var evidenceDirectory = configuration["FinancialIntegrations:EvidenceDirectory"];
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            services.AddScoped(provider => new FinancialHttpEvidenceHandler(
                Path.Combine(Path.GetFullPath(evidenceDirectory), Guid.NewGuid().ToString("N")),
                new SharedFinancialTransportHandler(provider.GetRequiredService<HttpMessageInvoker>()),
                allowRepeatedWrites: true));
            services.AddScoped(provider => new NiboHttpClientFactory(
                provider.GetRequiredService<FinancialHttpEvidenceHandler>()));
        }
        else
        {
            services.AddSingleton(provider => new SharedFinancialTransportHandler(
                provider.GetRequiredService<HttpMessageInvoker>()));
            services.AddScoped(provider => new NiboHttpClientFactory(
                provider.GetRequiredService<SharedFinancialTransportHandler>()));
        }

        services.AddScoped<NiboConfigurationReader>();
    }
}
