using FluentValidation;

namespace YallaFinance.Application.Features.Debits.ListDebits;

public sealed class ListDebitsQueryValidator : AbstractValidator<ListDebitsQuery>
{
    public ListDebitsQueryValidator()
    {
        RuleFor(query => query.Filters).NotNull();
    }
}
