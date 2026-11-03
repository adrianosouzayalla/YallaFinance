using FluentValidation;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Email is the legacy transport name; the value identifies core.user.login only.
        RuleFor(command => command.Email).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Password).NotEmpty();
    }
}
