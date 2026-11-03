using YallaFinance.Application.Services.Authentication.Errors;


namespace YallaFinance.WebApi.Authentication;

public sealed class SessionFailureMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception error) when (!context.Response.HasStarted &&
            (error is SessionConfigurationException or SessionUnavailableException))
        {
            // No exception message or configuration material is returned to the browser.
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.CacheControl = "private, no-store";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "SESSION_UNAVAILABLE",
                detail = "Não foi possível acessar o serviço de sessão. Tente novamente."
            }, context.RequestAborted);
        }
    }
}
