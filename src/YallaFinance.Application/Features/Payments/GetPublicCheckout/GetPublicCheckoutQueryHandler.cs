using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.GetPublicCheckout;

public sealed class GetPublicCheckoutQueryHandler(IPaymentRepository service)
    : IRequestHandler<GetPublicCheckoutQuery, GetPublicCheckoutResult>
{
    /// <summary>
    /// Read-only. Card attempts still pending are confirmed by the provider reconciliation batch of the worker,
    /// so a public read never writes to the database or calls the card provider.
    /// </summary>
    public async Task<GetPublicCheckoutResult> Handle(GetPublicCheckoutQuery request, CancellationToken cancellationToken)
    {
        var result = await service.GetPublicCheckoutAsync(request.Token, cancellationToken);
        return new(result);
    }
}
