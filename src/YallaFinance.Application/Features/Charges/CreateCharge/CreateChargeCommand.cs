using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Payments;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed record CreateChargeCommand(
    ChargeSource Source,
    string CustomerId,
    IReadOnlyList<string> DebtIds,
    decimal Amount,
    DateOnly OccurrenceDate,
    string DescriptionDetail,
    DateOnly DueDate,
    string CustomerNote,
    string? SelectedMovementId,
    // Accepted only to replay legacy commands; never persisted on new charges.
    PaymentMethod PaymentMethod = default,
    string CategoryId = "",
    string? PaymentMethodId = null,
    string? IdempotencyKey = null) : IRequest<NotificationResult<ChargeDetailsResult>>;
