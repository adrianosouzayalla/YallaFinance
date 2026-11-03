using FluentValidation;

namespace YallaFinance.Application.Features.Users.SetUserActive;

public sealed class SetUserActiveCommandValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
    }
}
