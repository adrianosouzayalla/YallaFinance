using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace YallaFinance.Infrastructure.Providers;

internal interface IProviderModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
}
