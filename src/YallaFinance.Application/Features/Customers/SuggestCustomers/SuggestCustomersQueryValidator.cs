using FluentValidation;

namespace YallaFinance.Application.Features.Customers.SuggestCustomers;

public sealed class SuggestCustomersQueryValidator : AbstractValidator<SuggestCustomersQuery>
{
    public SuggestCustomersQueryValidator()
    {
        RuleFor(query => query.Source).Must(source => source is null or "all" or "persisted")
            .WithMessage("Fonte de clientes inválida.");
    }
}
