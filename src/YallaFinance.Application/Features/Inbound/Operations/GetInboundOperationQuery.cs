using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed record GetInboundOperationQuery(Guid OperationId) : IRequest<NotificationResult<InboundOperationResponse>>;
