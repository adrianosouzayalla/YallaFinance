using MediatR;
using System.Text.Json.Serialization;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Debits.CreateManualDebit;

public sealed record CreateManualDebitCommand(
    Guid CustomerId,
    Guid ContractId,
    Guid CategoryId,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
    DateOnly OccurrenceDate,
    DateOnly DueDate,
    string Description,
    Guid IdempotencyKey)
    : IRequest<NotificationResult<CreateManualDebitResult>>;
