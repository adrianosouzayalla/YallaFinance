using FluentValidation;

namespace YallaFinance.Application.Features.Debits.GetDebit;

public sealed class GetDebitQueryValidator : AbstractValidator<GetDebitQuery>
{
    public GetDebitQueryValidator()
    {
        // Invalid identifiers are treated as missing debits by the repository and handler.
    }
}
