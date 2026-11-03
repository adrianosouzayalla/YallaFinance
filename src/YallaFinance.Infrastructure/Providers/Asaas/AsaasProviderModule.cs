using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Domain.Payments;
using YallaFinance.Infrastructure.Providers.Asaas.Payments;
using YallaFinance.Infrastructure.Providers.Asaas.Payments.Boleto;
using YallaFinance.Infrastructure.Providers.Asaas.Payments.Pix;

namespace YallaFinance.Infrastructure.Providers.Asaas;

internal sealed class AsaasProviderModule : IProviderModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // Dedicated transport: does not replace Nibo/Sicoob transports or add shared HTTP policies.
        services.TryAddSingleton<AsaasClient>(_ => new AsaasClient());
        services.TryAddScoped<AsaasConfigurationReader>();
        services.TryAddScoped<AsaasApiTransport>(serviceProvider => new AsaasApiTransport(
            serviceProvider.GetRequiredService<AsaasConfigurationReader>(),
            serviceProvider.GetRequiredService<AsaasClient>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .CreateLogger("YallaFinance.Infrastructure.Providers.Asaas")));
        services.TryAddScoped<AsaasPaymentOperations>();
        services.TryAddScoped<AsaasCustomerOperations>();
        services.TryAddScoped<AsaasPixOperations>();
        services.TryAddScoped<AsaasBoletoPaymentProvider>();
        services.AddSingleton(new CompanyPaymentProviderCapabilities("Asaas",
            [PaymentInstrumentType.CreditCard, PaymentInstrumentType.Pix, PaymentInstrumentType.Boleto]));
    }
}
