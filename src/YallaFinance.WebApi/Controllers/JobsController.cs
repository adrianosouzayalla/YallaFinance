using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Jobs.ListJobs;
using YallaFinance.Application.Features.Jobs.GetJob;
using YallaFinance.Application.Features.Jobs.ListJobExecutions;
using YallaFinance.Application.Features.Jobs.ListJobTypes;
using YallaFinance.Application.Features.Jobs.ReprocessJob;
using YallaFinance.WebApi.Extensions;
using YallaFinance.WebApi.Authorization;
namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[RequireFunctionality("JOBS")]
[Route("api/jobs")]
public sealed class JobsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id}/reprocess")]
    public async Task<IActionResult> Reprocess(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new ReprocessJobCommand(id), cancellationToken)).ToActionResult(this);

    [HttpGet("types")]
    public async Task<IActionResult> Types(CancellationToken cancellationToken) =>
        (await mediator.Send(new ListJobTypesQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ListJobsQuery query,
        CancellationToken cancellationToken) =>
        (await mediator.Send(query, cancellationToken)).ToActionResult(this);

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetJobQuery(id), cancellationToken)).ToActionResult(this);

    [HttpGet("{id}/executions")]
    public async Task<IActionResult> Executions(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc") =>
        (await mediator.Send(
            new ListJobExecutionsQuery(id, page, pageSize, sortBy, sortDirection),
            cancellationToken)).ToActionResult(this);
}
