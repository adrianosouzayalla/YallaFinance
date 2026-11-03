using FluentValidation;

namespace YallaFinance.Application.Features.AccessProfiles.SetAccessProfileActive;

public sealed class SetAccessProfileActiveCommandValidator : AbstractValidator<SetAccessProfileActiveCommand>
{
    public SetAccessProfileActiveCommandValidator()
    {
        // The service owns profile lookup and preserves its not-found response.
    }
}
