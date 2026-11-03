using FluentValidation;

namespace YallaFinance.Application.Features.Payments.StartPublicPaymentAttempt;

public sealed class StartPublicPaymentAttemptCommandValidator : AbstractValidator<StartPublicPaymentAttemptCommand>
{
    public StartPublicPaymentAttemptCommandValidator()
    {
        RuleFor(command => command.Token).Must(token => token is { Length: 64 } &&
            token.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
            .WithMessage("Não foi possível iniciar este pagamento. Confira os dados e tente novamente.");
        RuleFor(command => command.MethodCode).Must(code => !string.IsNullOrWhiteSpace(code) && code.Length <= 64)
            .WithMessage("Não foi possível iniciar este pagamento. Confira os dados e tente novamente.");
        RuleFor(command => command.IdempotencyKey).Must(key => !string.IsNullOrWhiteSpace(key) && key.Length <= 200)
            .WithMessage("Não foi possível iniciar este pagamento. Confira os dados e tente novamente.");
    }
}
