using FluentValidation;

namespace YallaFinance.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
    }
}
