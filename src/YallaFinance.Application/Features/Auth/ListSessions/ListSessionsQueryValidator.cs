using FluentValidation;
using FluentValidation.Results;
using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Auth.ListSessions;

public sealed class ListSessionsQueryValidator : AbstractValidator<ListSessionsQuery>
{
    public ListSessionsQueryValidator()
    {
        RuleFor(query => query.Filter).Must(IsValidFilter)
            .WithMessage("Paginação ou ordenação inválida.");
    }

    // This validation was synchronous before checking session context, including
    // when the request token was cancelled. Preserve that order; the handler
    // forwards the original cancellation token to the session service.
    public override Task<ValidationResult> ValidateAsync(ValidationContext<ListSessionsQuery> context,
        CancellationToken cancellation = default) => Task.FromResult(Validate(context));

    private static bool IsValidFilter(ActiveSessionFilter filter)
    {
        try
        {
            filter.Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
