namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialReceivable(
    string ProviderExternalId,
    string? Reference,
    string ProviderCustomerId,
    decimal Amount,
    DateOnly ScheduleDate,
    DateOnly DueDate,
    DateOnly AccrualDate,
    string? Description,
    IReadOnlyList<FinancialReceivableCategory> Categories,
    bool IsPaid,
    bool IsActive = true);
