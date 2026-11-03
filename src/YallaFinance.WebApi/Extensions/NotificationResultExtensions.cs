using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Common.Results;

namespace YallaFinance.WebApi.Extensions;

public static class NotificationResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this NotificationResult<T> result,
        ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);
        var status = result.Errors.Any(error => error.Code == ErrorCodes.NotFound) ? StatusCodes.Status404NotFound
            : result.Errors.Any(error => error.Code == ErrorCodes.ServiceUnavailable) ? StatusCodes.Status503ServiceUnavailable
                : result.Errors.Any(error => error.Code == ErrorCodes.Forbidden) ? StatusCodes.Status403Forbidden
                    : result.Errors.Any(error => error.Code == ErrorCodes.Unauthorized) ? StatusCodes.Status401Unauthorized
                        : result.Errors.Any(error => error.Code == ErrorCodes.Conflict) ? StatusCodes.Status409Conflict
                            : StatusCodes.Status400BadRequest;
        return controller.StatusCode(status, new
        {
            errors = result.Errors
        });
    }
}
