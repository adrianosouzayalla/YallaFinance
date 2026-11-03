using FluentValidation;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.ListJobExecutions;

public sealed class ListJobExecutionsQueryValidator : AbstractValidator<ListJobExecutionsQuery>
{
    public ListJobExecutionsQueryValidator()
    {
        RuleFor(jobExecutionQuery => jobExecutionQuery.Id)
            .Must(jobIdValue => Guid.TryParse(jobIdValue, out var parsedJobId) && parsedJobId != Guid.Empty)
            .WithMessage("Identificador inválido.");
        RuleFor(jobExecutionQuery => jobExecutionQuery.SortBy)
            .Must(sortKey => sortKey is null or "id" or "status" or "started" or "finished" or "duration" or "error");
        RuleFor(jobExecutionQuery => jobExecutionQuery.SortDirection).Must(sortDirection => sortDirection is "asc" or "desc");
        RuleFor(jobExecutionQuery => jobExecutionQuery.Page).GreaterThan(0);
        RuleFor(jobExecutionQuery => jobExecutionQuery.PageSize).InclusiveBetween(1, 100);
        RuleFor(jobExecutionQuery => jobExecutionQuery)
            .Must(query => ((long)query.Page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("Página inválida.");
    }
}
