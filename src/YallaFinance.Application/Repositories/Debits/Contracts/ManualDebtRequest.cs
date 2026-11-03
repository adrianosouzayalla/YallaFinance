using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record ManualDebtRequest(
    Guid TenantId,
    Guid IdempotencyKey,
    string RequestFingerprint,
    Guid DebtId,
    DateTimeOffset CreatedAt);
