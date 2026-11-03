using FluentValidation;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.GetJob;

public sealed class GetJobQueryValidator : AbstractValidator<GetJobQuery>
{
    public GetJobQueryValidator()
    {
        RuleFor(jobQuery => jobQuery.Id)
            .Must(jobIdValue => Guid.TryParse(jobIdValue, out var parsedJobId) && parsedJobId != Guid.Empty)
            .WithMessage("Identificador inválido.");
    }
}
