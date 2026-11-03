using FluentValidation;

namespace YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;

public sealed class ListAccessProfilesQueryValidator : AbstractValidator<ListAccessProfilesQuery>
{
    public ListAccessProfilesQueryValidator()
    {
        RuleFor(query => query.Filter).NotNull();
        When(query => query.Filter is not null, () =>
        {
            RuleFor(query => query.Filter.Page).InclusiveBetween(1, 1_000_000)
                .WithMessage("Filtros, paginação ou ordenação inválidos.");
            RuleFor(query => query.Filter.PageSize).InclusiveBetween(1, 100)
                .WithMessage("Filtros, paginação ou ordenação inválidos.");
            RuleFor(query => query.Filter.SortBy).Must(sort => sort is "name" or "code" or "active")
                .WithMessage("Filtros, paginação ou ordenação inválidos.");
            RuleFor(query => query.Filter.SortDirection).Must(direction => direction is "asc" or "desc")
                .WithMessage("Filtros, paginação ou ordenação inválidos.");
            RuleFor(query => query.Filter.Search).MaximumLength(200)
                .WithMessage("Filtro muito longo.");
        });
    }
}
