using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Providers.Payments.Pix;

internal interface IPixChargeProvider : IProviderCapability
{
    // Throw caller cancellation only before possible dispatch; after dispatch return Indeterminate.
    Task<CreatePixChargeResult> CreateAsync(
        CreatePixChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken);
    Task<PixChargeQueryResult> GetAsync(
        PixChargeQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken);
}
