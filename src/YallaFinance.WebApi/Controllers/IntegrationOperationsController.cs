using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Inbound.Operations;
using YallaFinance.WebApi.Extensions;
namespace YallaFinance.WebApi.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Route("api/integrations/operations")]
public sealed class IntegrationOperationsController(ISender sender) : ControllerBase
{
    [HttpGet("{operationId}")]
    public async Task<IActionResult> Get(Guid operationId, CancellationToken cancellationToken) =>
        (await sender.Send(new GetInboundOperationQuery(operationId), cancellationToken)).ToActionResult(this);
}
