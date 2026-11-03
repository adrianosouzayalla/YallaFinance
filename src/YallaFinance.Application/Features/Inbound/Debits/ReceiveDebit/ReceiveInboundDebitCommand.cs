using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ReceiveInboundDebitCommand(ReceiveDebitPayload Payload) : IRequest<NotificationResult<ReceiveInboundDebitReceipt>>;
