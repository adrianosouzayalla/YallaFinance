using FluentValidation;

namespace YallaFinance.Application.Features.Customers.FindMovements;

public sealed class FindMovementsQueryValidator : AbstractValidator<FindMovementsQuery>
{
    public FindMovementsQueryValidator()
    {
        // Unknown customer identifiers and dates preserve the existing empty-result behavior.
    }
}
