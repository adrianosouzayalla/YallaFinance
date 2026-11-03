using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Payments;
using YallaFinance.Application;

namespace YallaFinance.Application.Services.Charges.Contracts;

public sealed record ChargeRequest(
    ChargeSource Source,
    string TenantId,
    string CustomerId,
    IReadOnlyList<string> DebtIds,
    decimal Amount,
    DateOnly OccurrenceDate,
    string DescriptionDetail,
    DateOnly DueDate,
    string CustomerNote,
    string? SelectedMovementId,
    PaymentMethod PaymentMethod,
    string CategoryId,
    string PaymentMethodId);
