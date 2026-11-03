using FluentValidation;
using YallaFinance.Domain.Charges;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed class CreateChargeCommandValidator : AbstractValidator<CreateChargeCommand>
{
    public CreateChargeCommandValidator()
    {
        RuleFor(command => command.Source).Must(source => source is ChargeSource.Selected or ChargeSource.Standalone)
            .WithMessage("A criação padrão aceita apenas cobrança avulsa ou débitos selecionados.");
        RuleFor(command => command.CustomerId).NotEmpty().Must(customerId => Guid.TryParse(customerId, out _));
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.DescriptionDetail).NotEmpty().When(command => command.Source == ChargeSource.Standalone);
        // Relative date validation belongs after the persisted replay check.
        RuleFor(command => command.DueDate).NotEqual(default(DateOnly));
        RuleFor(command => command.CategoryId).NotEmpty().When(command => command.Source == ChargeSource.Standalone);
        RuleFor(command => command.IdempotencyKey).NotEmpty();
        RuleFor(command => command.DebtIds).NotEmpty().When(command => command.Source == ChargeSource.Selected);
        RuleFor(command => command.SelectedMovementId).NotEmpty().Must(movementId => Guid.TryParse(movementId, out _))
            .When(command => command.Source == ChargeSource.Standalone);
        RuleFor(command => command.OccurrenceDate).NotEqual(default(DateOnly)).When(command => command.Source == ChargeSource.Standalone);
    }

    internal static DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "America/Sao_Paulo").DateTime);
}
