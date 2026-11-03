using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Debits.GetDebit;
using YallaFinance.Application.Features.Debits.ListDebits;
using YallaFinance.Application.Features.Debits.CreateManualDebit;

using YallaFinance.WebApi.Extensions;
using YallaFinance.Domain.Debts;
using YallaFinance.WebApi.Authorization;
using YallaFinance.Application.Repositories.Debits.Contracts;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/debts")]
public sealed class DebtsController(IMediator mediator) : ControllerBase
{
    [HttpPost, RequireFunctionality("DEBTS_CREATE")]
    public async Task<IActionResult> Create(
        [FromBody] CreateManualDebitCommand request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);
        return StatusCode(result.Value!.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            new
            {
                debtId = result.Value.DebtId
            });
    }

    [HttpGet, RequireFunctionality("DEBTS")]
    public async Task<IActionResult> List(
        [FromQuery] DebtFiltersRequest request,
        CancellationToken cancellationToken)
    {
        if (Request.Query.TryGetValue("status", out var status)
            && (status.Count != 1 || !Enum.GetNames<DebtStatus>().Contains(
                status.ToString(),
                StringComparer.Ordinal)))
            return BadRequest(new
            {
                errors = new[] { new { code = "Validation", field = "status", message = "Status inválido. Use Open, InCollection, Paid ou Canceled." } }
            });
        return (await mediator.Send(new ListDebitsQuery(request), cancellationToken)).ToActionResult(this);
    }

    [HttpGet("{id}"), RequireFunctionality("DEBTS", "CHARGES_CREATE")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetDebitQuery(id), cancellationToken)).ToActionResult(this);
}
