using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.GetPaymentRequest;

public sealed class GetPaymentRequestQueryHandler(IPaymentRepository service)
    : IRequestHandler<GetPaymentRequestQuery, GetPaymentRequestResult>
{
    public async Task<GetPaymentRequestResult> Handle(GetPaymentRequestQuery request, CancellationToken cancellationToken) =>
        new(await service.GetAsync(request.TenantId, request.RequestId, cancellationToken));
}
