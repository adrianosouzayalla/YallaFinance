using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YallaFinance.WebApi.Authentication;

public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException validationException)
            return;

        if (UsesUserManagementErrorContract(context))
        {
            var detail = validationException.Errors.FirstOrDefault()?.ErrorMessage ?? "Dados inválidos.";
            context.HttpContext.Response.Headers.CacheControl = "private, no-store";
            context.Result = new ObjectResult(new { code = "INVALID_USER_INPUT", detail })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            context.ExceptionHandled = true;
            return;
        }

        var errors = validationException.Errors
            .Select(error => new
            {
                code = "Validation",
                field = error.PropertyName,
                message = error.ErrorMessage
            })
            .ToArray();

        context.HttpContext.Response.Headers.CacheControl = "private, no-store";
        context.Result = new BadRequestObjectResult(new { errors });
        context.ExceptionHandled = true;
    }

    private static bool UsesUserManagementErrorContract(ExceptionContext context)
    {
        var routeValues = context.ActionDescriptor.RouteValues;
        routeValues.TryGetValue("controller", out var controller);
        routeValues.TryGetValue("action", out var action);

        return controller is "Users" or "AccessProfiles"
            || controller == "Auth" && action == "ChangePassword";
    }
}
