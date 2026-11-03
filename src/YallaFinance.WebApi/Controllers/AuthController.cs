using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Auth.GetSession;
using YallaFinance.Application.Features.Auth.Login;
using YallaFinance.Application.Features.Auth.Logout;
using YallaFinance.Application.Features.Auth.ListSessions;
using YallaFinance.Application.Features.Auth.ChangePassword;
using YallaFinance.Application.Common.Results;
using YallaFinance.WebApi.Extensions;

using YallaFinance.WebApi.Authentication;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Security;
using YallaFinance.Application.Repositories.Sessions.Contracts;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [TypeFilter(typeof(AuthenticationAttemptFilter), Arguments = new object[] { AuthenticationAttemptKind.Login })]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken) =>
        (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpGet("session")]
    [Authorize]
    public async Task<IActionResult> Session(CancellationToken cancellationToken) =>
        (await mediator.Send(
            new GetSessionQuery(),
            cancellationToken)).ToActionResult(this);

    [HttpPost("logout")]
    [Authorize]
    [AllowRevokedSession]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        (await mediator.Send(new LogoutCommand(), cancellationToken)).ToActionResult(this);

    [HttpGet("sessions"), RequireFunctionality("SESSIONS")]
    [Authorize]
    public async Task<IActionResult> Sessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var filter = new ActiveSessionFilter(page, pageSize, sortBy, sortDirection);
        // Keep the legacy cache header when the session service throws, while
        // preserving the previous HTTP headers for rejected filters/context.
        var hadCacheControl = Response.Headers.TryGetValue("Cache-Control", out var previousCacheControl);
        Response.Headers.CacheControl = "private, no-store";
        var result = await mediator.Send(new ListSessionsQuery(filter), cancellationToken);
        if (result.IsFailure)
        {
            if (hadCacheControl) Response.Headers.CacheControl = previousCacheControl;
            else Response.Headers.Remove("Cache-Control");
            if (result.Errors.Any(error => error.Code == ErrorCodes.Validation))
                return BadRequest(new { code = "INVALID_SESSION_QUERY", detail = "Paginação ou ordenação inválida." });
            return Unauthorized();
        }
        return Ok(result.Value!.Page);
    }

    [HttpPost("password")]
    [Authorize]
    [TypeFilter(typeof(UserManagementExceptionFilter))]
    public async Task<IActionResult> ChangePassword(
        YallaFinance.Application.Features.Auth.ChangePassword.PasswordInput input,
        CancellationToken ct)
    {
        await mediator.Send(new ChangePasswordCommand(input), ct);
        return NoContent();
    }

}
