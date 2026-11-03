using FluentValidation;

namespace YallaFinance.Application.Features.Payments.GetPublicCheckout;

public sealed class GetPublicCheckoutQueryValidator : AbstractValidator<GetPublicCheckoutQuery>
{
    public GetPublicCheckoutQueryValidator()
    {
        // Opaque public tokens intentionally share the same not-found response when malformed or unknown.
    }
}
