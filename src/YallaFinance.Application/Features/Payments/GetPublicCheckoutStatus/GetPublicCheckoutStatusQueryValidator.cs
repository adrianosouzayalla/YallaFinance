using FluentValidation;

namespace YallaFinance.Application.Features.Payments.GetPublicCheckoutStatus;

public sealed class GetPublicCheckoutStatusQueryValidator : AbstractValidator<GetPublicCheckoutStatusQuery>
{
    public GetPublicCheckoutStatusQueryValidator()
    {
        // Opaque public tokens intentionally share the same not-found response when malformed or unknown.
    }
}
