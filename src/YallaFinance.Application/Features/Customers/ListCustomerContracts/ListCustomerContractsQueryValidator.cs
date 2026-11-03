using FluentValidation;

namespace YallaFinance.Application.Features.Customers.ListCustomerContracts;

public sealed class ListCustomerContractsQueryValidator : AbstractValidator<ListCustomerContractsQuery>
{
    public ListCustomerContractsQueryValidator()
    {
        RuleFor(query => query.CustomerId).NotEmpty()
            .WithMessage("Confira cliente e paginação.");
        RuleFor(query => query.Page).GreaterThan(0)
            .WithMessage("Confira cliente e paginação.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, 20)
            .WithMessage("Confira cliente e paginação.");
        RuleFor(query => query.OccurrenceDate).Must(date => date is null || date != DateOnly.MinValue)
            .WithMessage("Confira cliente e paginação.");
    }
}
