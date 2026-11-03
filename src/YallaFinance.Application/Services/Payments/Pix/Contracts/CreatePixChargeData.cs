using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

/// <summary>The caller owns durable intent, reference, authorization and concurrency before invoking the integration.</summary>
public sealed record CreatePixChargeData(
    Guid TenantId,
    Guid ConnectionId,
    string ExpectedProvider,
    Guid OperationId,
    string ProviderReference,
    decimal Amount,
    int ExpirationSeconds,
    PixChargePayer? Payer = null,
    string? Description = null,
    DateOnly? DueDate = null);
