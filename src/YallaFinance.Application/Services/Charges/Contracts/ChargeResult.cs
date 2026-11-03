using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Payments;
using YallaFinance.Application;

namespace YallaFinance.Application.Services.Charges.Contracts;

public sealed record ChargeResult(
    string Id,
    string IdempotencyKey,
    ChargeSource Source,
    string TenantId,
    string CustomerId,
    IReadOnlyList<string> DebtIds,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string PaymentMethodId,
    string CategoryId,
    DateOnly DueDate,
    string ProviderExternalId,
    string PixId,
    string PixPayload,
    DateTimeOffset CreatedAt);
