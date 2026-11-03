using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application.Features.Charges.CreateDirectCharge;
using YallaFinance.Application.Features.Charges.GetCharge;
using YallaFinance.WebApi.Extensions;
using YallaFinance.WebApi.Authorization;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/charges")]
public sealed class ChargesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}"), RequireFunctionality("CHARGES")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetChargeQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost, RequireFunctionality("CHARGES_CREATE")]
    public async Task<IActionResult> Create(CreateChargeCommand command, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var result = await mediator.Send(command with
        {
            IdempotencyKey = idempotencyKey
        }, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("direct"), RequireFunctionality("DIRECT_CHARGES_CREATE")]
    public async Task<IActionResult> CreateDirectCharge(CreateDirectChargeCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with
        {
            IdempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault()
        }, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("direct/{id:guid}/status"), RequireFunctionality("DIRECT_CHARGES_CREATE")]
    public async Task<IActionResult> GetDirectChargeStatus(Guid id, CancellationToken cancellationToken)
        => (await mediator.Send(new GetDirectChargeStatusQuery(id), cancellationToken)).ToActionResult(this);
}
