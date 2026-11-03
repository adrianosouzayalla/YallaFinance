using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Dashboard.GetSummary;
using YallaFinance.WebApi.Extensions;
using YallaFinance.WebApi.Authorization;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary"), RequireFunctionality("DEBTS")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        (await mediator.Send(new GetSummaryQuery(), cancellationToken)).ToActionResult(this);
}
