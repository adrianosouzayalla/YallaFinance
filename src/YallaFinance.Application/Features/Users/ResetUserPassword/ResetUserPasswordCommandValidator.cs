using FluentValidation;

namespace YallaFinance.Application.Features.Users.ResetUserPassword;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        // The service enforces self-reset and confirmation in the established order.
    }
}
