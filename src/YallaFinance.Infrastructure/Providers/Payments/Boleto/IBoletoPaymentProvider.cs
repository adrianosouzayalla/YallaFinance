using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Infrastructure.Providers.Configuration;

namespace YallaFinance.Infrastructure.Providers.Payments.Boleto;

internal interface IBoletoPaymentProvider : IProviderCapability
{
    Task<BoletoProviderResult<BoletoCharge>> CreateAsync(
        CreateBoletoChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken);

    Task<BoletoProviderResult<BoletoCharge>> GetAsync(
        BoletoChargeQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken);
}

internal enum BoletoProviderOutcome { Completed, Rejected, Indeterminate, ConfigurationError, NotFound }

internal sealed record BoletoProviderResult<T>(BoletoProviderOutcome Outcome, T? Value, string? ErrorCode = null)
{
    public string? FailureMessage { get; init; }
}
