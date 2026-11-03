using System.Net.Mail;
using FluentValidation;

namespace YallaFinance.Application.Features.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
        When(command => command.Input is not null, () =>
        {
            RuleFor(command => command.Input.Login)
                .Must(login => login is not null && login.Trim().Length is >= 3 and <= 100 &&
                    !login.Trim().Any(char.IsWhiteSpace))
                .WithMessage("Login deve ter de 3 a 100 caracteres, sem espaços.");
            RuleFor(command => command.Input.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 200)
                .WithMessage("Nome deve ter de 1 a 200 caracteres.");
            RuleFor(command => command.Input.Email).Must(IsValidEmail)
                .WithMessage("Informe um e-mail válido.");
        });
    }

    private static bool IsValidEmail(string? value)
    {
        var email = value?.Trim().ToLowerInvariant();
        return email is { Length: <= 254 } && MailAddress.TryCreate(email, out var address) &&
            address.Address == email && !email.Any(char.IsWhiteSpace);
    }
}
