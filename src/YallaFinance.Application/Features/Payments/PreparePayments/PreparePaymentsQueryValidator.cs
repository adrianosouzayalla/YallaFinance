using FluentValidation;

namespace YallaFinance.Application.Features.Payments.PreparePayments;

public sealed class PreparePaymentsQueryValidator : AbstractValidator<PreparePaymentsQuery>
{
    public PreparePaymentsQueryValidator()
    {
        RuleFor(query => query.ChargeIds).NotNull()
            .Must(ids => ids is { Count: >= 1 and <= 100 } && !ids.Contains(Guid.Empty) &&
                ids.Distinct().Count() == ids.Count)
            .WithMessage("Selecione de 1 a 100 cobranças distintas.");
    }
}
