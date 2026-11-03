using FluentValidation;

namespace YallaFinance.Application.Features.Customers.SearchCustomers;

public sealed class SearchCustomersQueryValidator : AbstractValidator<SearchCustomersQuery>
{
    public SearchCustomersQueryValidator() =>
        RuleFor(query => query.Search).NotNull();
}
