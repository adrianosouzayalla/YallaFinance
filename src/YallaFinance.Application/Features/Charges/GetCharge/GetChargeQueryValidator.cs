using FluentValidation;

namespace YallaFinance.Application.Features.Charges.GetCharge;

public sealed class GetChargeQueryValidator : AbstractValidator<GetChargeQuery>
{
    public GetChargeQueryValidator()
    {
        // Invalid identifiers are treated as missing charges by the repository and handler.
    }
}
