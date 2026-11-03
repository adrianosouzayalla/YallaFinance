using System.Net.Mail;
using FluentValidation;

namespace YallaFinance.Application.Features.Users.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
        When(command => command.Input is not null, () =>
        {
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
