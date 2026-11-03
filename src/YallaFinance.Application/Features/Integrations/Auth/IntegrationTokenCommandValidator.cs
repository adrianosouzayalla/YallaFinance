using FluentValidation;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Integrations.Auth;

public sealed class IntegrationTokenCommandValidator : AbstractValidator<IntegrationTokenCommand>
{
    public IntegrationTokenCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}
