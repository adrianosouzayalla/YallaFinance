using FluentValidation;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public sealed class ReprocessJobCommandValidator : AbstractValidator<ReprocessJobCommand>
{
    public ReprocessJobCommandValidator()
    {
        RuleFor(command => command.Id)
            .Must(jobIdValue => Guid.TryParse(jobIdValue, out var parsedJobId) && parsedJobId != Guid.Empty)
            .WithMessage("Identificador inválido.");
    }
}
