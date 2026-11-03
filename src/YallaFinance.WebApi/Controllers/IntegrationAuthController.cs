using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Auth.Login;
using YallaFinance.Application.Features.Integrations.Auth;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[Route("api/integrations/auth")]
public sealed class IntegrationAuthController(ISender sender) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Token(
        IntegrationTokenCommand command,
        CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult(this);
}
