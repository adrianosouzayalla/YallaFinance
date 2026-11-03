using FluentValidation;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.ListJobs;

public sealed class ListJobsQueryValidator : AbstractValidator<ListJobsQuery>
{
    public ListJobsQueryValidator()
    {
        RuleFor(jobListQuery => jobListQuery.Key).MaximumLength(250);
        RuleFor(jobListQuery => jobListQuery).Must(jobListQuery => jobListQuery.StartDate is null || jobListQuery.EndDate is null || jobListQuery.StartDate < jobListQuery.EndDate)
            .WithMessage("A data fim deve ser posterior ao início do período.");
        RuleFor(jobListQuery => jobListQuery.SortBy).Must(sortKey => sortKey is null or "key" or "type" or "status" or "created" or "started" or "duration");
        RuleFor(jobListQuery => jobListQuery.SortDirection).Must(sortDirection => sortDirection is "asc" or "desc");
        RuleFor(jobListQuery => jobListQuery.Type).MaximumLength(100);
        RuleFor(jobListQuery => jobListQuery.Status).Must(jobStatus => jobStatus is null or "Pending" or "Processing" or "Success" or "Failed")
            .WithMessage("Status inválido.");
        RuleFor(jobListQuery => jobListQuery.Page).GreaterThan(0);
        RuleFor(jobListQuery => jobListQuery.PageSize).InclusiveBetween(1, 100);
        RuleFor(jobListQuery => jobListQuery).Must(jobListQuery => ((long)jobListQuery.Page - 1) * jobListQuery.PageSize <= int.MaxValue).WithMessage("Página inválida.");
    }
}
