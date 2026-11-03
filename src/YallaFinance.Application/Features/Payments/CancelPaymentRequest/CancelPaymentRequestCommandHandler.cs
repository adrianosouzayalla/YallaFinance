using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.CancelPaymentRequest;

public sealed class CancelPaymentRequestCommandHandler(IPaymentRepository service)
    : IRequestHandler<CancelPaymentRequestCommand, CancelPaymentRequestResult>
{
    public async Task<CancelPaymentRequestResult> Handle(CancelPaymentRequestCommand request, CancellationToken cancellationToken) =>
        new(await service.CancelAsync(request.TenantId, request.RequestId, cancellationToken));
}
