using FluentValidation;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Debits.CreateManualDebit;

public sealed class CreateManualDebitCommandValidator : AbstractValidator<CreateManualDebitCommand>
{
    public CreateManualDebitCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ContractId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0).LessThanOrEqualTo(9_999_999_999_999_999.99m).Must(amount => decimal.Round(
            amount,
            2) == amount);
        RuleFor(command => command.OccurrenceDate).NotEqual(default(DateOnly));
        RuleFor(command => command.DueDate).NotEqual(default(DateOnly));
        RuleFor(command => command.Description).NotEmpty();
    }
}
