using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.GetPublicCheckout;

namespace YallaFinance.Application.Features.Payments.GetPublicCheckoutStatus;

/// <summary>
/// Reads the public checkout through the same query, so the card recovery that runs there also runs
/// for status checks. Only the status fields are returned.
/// </summary>
public sealed class GetPublicCheckoutStatusQueryHandler(
    IRequestHandler<GetPublicCheckoutQuery, GetPublicCheckoutResult> checkoutQuery)
    : IRequestHandler<GetPublicCheckoutStatusQuery, GetPublicCheckoutStatusResult>
{
    public async Task<GetPublicCheckoutStatusResult> Handle(
        GetPublicCheckoutStatusQuery request,
        CancellationToken cancellationToken)
    {
        var checkout = await checkoutQuery.Handle(new GetPublicCheckoutQuery(request.Token), cancellationToken);
        var response = checkout.Response.IsSuccess && checkout.Response.Value is not null
            ? NotificationResult<PublicPaymentCheckoutStatus>.Success(
                PublicPaymentCheckoutStatus.From(checkout.Response.Value))
            : NotificationResult<PublicPaymentCheckoutStatus>.Failure(checkout.Response.Errors);
        return new(response);
    }
}
