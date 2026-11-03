using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed record InboundOperationResponse(
    Guid OperationId,
    string Status,
    string Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    InboundOperationResource? Resource,
    InboundOperationError? Error);
