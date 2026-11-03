using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using YallaFinance.Application.Repositories.Authorization;
using YallaFinance.Application.Common.Context;


namespace YallaFinance.WebApi.Authorization;

public sealed class FunctionalityAuthorizationHandler(IFunctionalityAccessRepository access, CurrentSessionContext current)
    : AuthorizationHandler<FunctionalityRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FunctionalityRequirement requirement)
    {
        if (current.Value is not { } session) return;
        var cancellationToken = context.Resource switch
        {
            HttpContext http => http.RequestAborted,
            AuthorizationFilterContext filter => filter.HttpContext.RequestAborted,
            _ => CancellationToken.None
        };
        if (await access.HasAnyAsync(session, requirement.Codes, cancellationToken))
            context.Succeed(requirement);
    }
}
