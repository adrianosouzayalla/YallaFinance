using YallaFinance.Domain.Debts;
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record DebtResponse(
    string Id,
    string? CategoryId,
    string TenantId,
    string TenantName,
    string Contract,
    string Product,
    string Plate,
    string Fleet,
    DateOnly OccurrenceDate,
    decimal Amount,
    DebtStatus Status,
    string Description,
    string Source,
    string? ProviderExternalId,
    DateOnly? DueDate = null,
    string? CustomerId = null,
    string? ChargeId = null);
