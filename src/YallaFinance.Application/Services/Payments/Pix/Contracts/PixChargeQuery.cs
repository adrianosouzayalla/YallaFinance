using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixChargeQuery(
    Guid TenantId,
    Guid ConnectionId,
    string ExpectedProvider,
    string ProviderReference,
    DateTimeOffset? AttemptCreatedAt = null);
