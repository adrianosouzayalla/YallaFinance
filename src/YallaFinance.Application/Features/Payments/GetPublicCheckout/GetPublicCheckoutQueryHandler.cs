using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.GetPublicCheckout;

public sealed class GetPublicCheckoutQueryHandler(IPaymentRepository service,
    YallaFinance.Application.Services.Payments.IPublicPaymentAttemptService? attempts = null)
    : IRequestHandler<GetPublicCheckoutQuery, GetPublicCheckoutResult>
{
    public async Task<GetPublicCheckoutResult> Handle(GetPublicCheckoutQuery request, CancellationToken cancellationToken)
    {
        var result = await service.GetPublicCheckoutAsync(request.Token, cancellationToken);
        if (attempts is not null && result.IsSuccess && result.Value?.Attempt is
            { InstrumentType: "CreditCard", PaymentMethodCode: { } code, AttemptReference: { } reference,
              Status: "Unknown" or "AwaitingPayment" or "Processing" })
        {
            // No card input: the reservation can only recover an existing attempt, never create/pay one.
            await attempts.StartOrGetAsync(request.Token, code, $"card-status-query:{reference}", cancellationToken);
            result = await service.GetPublicCheckoutAsync(request.Token, cancellationToken);
        }
        return new(result);
    }
}
