using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.RecoverPaymentLink;

public sealed class RecoverPaymentLinkQueryHandler(IPaymentRepository service)
    : IRequestHandler<RecoverPaymentLinkQuery, RecoverPaymentLinkResult>
{
    public async Task<RecoverPaymentLinkResult> Handle(RecoverPaymentLinkQuery request, CancellationToken cancellationToken) =>
        new(await service.RecoverLinkAsync(request.TenantId, request.RequestId, cancellationToken));
}
