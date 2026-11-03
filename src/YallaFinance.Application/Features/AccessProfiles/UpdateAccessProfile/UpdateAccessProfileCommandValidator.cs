using FluentValidation;

namespace YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;

public sealed class UpdateAccessProfileCommandValidator : AbstractValidator<UpdateAccessProfileCommand>
{
    public UpdateAccessProfileCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
        When(command => command.Input is not null, () =>
        {
            // A missing functionality selection has a legacy service error code;
            // preserve that precedence by validating the name only when supplied.
            When(command => command.Input.FunctionalityIds is not null, () =>
                RuleFor(command => command.Input.Name).Must(name =>
                        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 100)
                    .WithMessage("Nome deve ter de 1 a 100 caracteres."));
        });
    }
}
