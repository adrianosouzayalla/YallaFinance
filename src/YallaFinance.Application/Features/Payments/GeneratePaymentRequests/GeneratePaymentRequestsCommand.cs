using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Payments.GeneratePaymentRequests;

public sealed record GeneratePaymentRequestsCommand(GeneratePaymentRequestsData Data) : IRequest<NotificationResult<PaymentRequestBatchResult>>;
