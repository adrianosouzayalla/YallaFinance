using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.PaymentMethods.Contracts;

public sealed record PaymentMethodResponse(
    string Id,
    string Code,
    string Name,
    string TenantId,
    bool Active,
    string? InstrumentType = null,
    string? InternalDescription = null);
