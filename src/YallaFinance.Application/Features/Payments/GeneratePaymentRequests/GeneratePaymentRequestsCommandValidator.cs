using FluentValidation;

namespace YallaFinance.Application.Features.Payments.GeneratePaymentRequests;

public sealed class GeneratePaymentRequestsCommandValidator : AbstractValidator<GeneratePaymentRequestsCommand>
{
    public GeneratePaymentRequestsCommandValidator()
    {
        RuleFor(command => command.Data).NotNull();
        When(command => command.Data is not null, () =>
        {
            RuleFor(command => command.Data.Items)
                .NotNull().Must(items => items is { Count: >= 1 and <= 100 })
                .WithMessage("Selecione de 1 a 100 cobranças distintas e confira o formulário.");
            When(command => command.Data.Items is not null, () =>
            {
                RuleFor(command => command.Data.Items).Must(items =>
                        items.All(item => item is not null && item.ChargeId != Guid.Empty &&
                            !string.IsNullOrWhiteSpace(item.IdempotencyKey) && item.IdempotencyKey.Length <= 200) &&
                        items.Select(item => item.ChargeId).Distinct().Count() == items.Count)
                    .WithMessage("Selecione de 1 a 100 cobranças distintas e confira o formulário.");
            });
            RuleFor(command => command.Data.PaymentMethodIds)
                .NotNull().Must(ids => ids is { Count: > 0 } && !ids.Contains(Guid.Empty) &&
                    ids.Distinct().Count() == ids.Count)
                .WithMessage("Selecione de 1 a 100 cobranças distintas e confira o formulário.");
            RuleFor(command => command.Data.Note).MaximumLength(2000)
                .WithMessage("Selecione de 1 a 100 cobranças distintas e confira o formulário.");
        });
    }
}
