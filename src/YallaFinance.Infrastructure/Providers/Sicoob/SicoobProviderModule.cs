using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Domain.Payments;

namespace YallaFinance.Infrastructure.Providers.Sicoob;

internal sealed class SicoobProviderModule : IProviderModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<SicoobHttpClientFactory>(_ => new SicoobHttpClientFactory());
        services.TryAddSingleton<SicoobAuthentication>();
        services.TryAddScoped<SicoobConfigurationReader>();
        services.AddSingleton(new CompanyPaymentProviderCapabilities("Sicoob", [PaymentInstrumentType.Pix]));
    }
}
