using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace YallaFinance.WebApi.Authorization;

public sealed class FunctionalityAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && policy.Requirements.Any(requirement => requirement is FunctionalityRequirement))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "FORBIDDEN",
                detail = "Você não tem acesso a esta funcionalidade."
            }, context.RequestAborted);
            return;
        }

        await fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
