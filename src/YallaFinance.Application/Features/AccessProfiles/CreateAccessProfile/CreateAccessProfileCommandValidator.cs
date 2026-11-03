using FluentValidation;

namespace YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;

public sealed class CreateAccessProfileCommandValidator : AbstractValidator<CreateAccessProfileCommand>
{
    public CreateAccessProfileCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
        When(command => command.Input is not null, () =>
        {
            RuleFor(command => command.Input.Code).Must(code =>
                    !string.IsNullOrWhiteSpace(code) &&
                    System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), @"\A[A-Za-z0-9_]{2,50}\z"))
                .WithMessage("Código deve ter de 2 a 50 letras, números ou underscore.");
            RuleFor(command => command.Input.Name).Must(name =>
                    !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 100)
                .WithMessage("Nome deve ter de 1 a 100 caracteres.");
        });
    }
}
