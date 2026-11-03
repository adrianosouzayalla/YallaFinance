using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Categories.ListCategories;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet, RequireFunctionality("DEBTS", "DEBTS_CREATE", "CHARGES", "CHARGES_CREATE", "DIRECT_CHARGES_CREATE")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await mediator.Send(new ListCategoriesQuery(), cancellationToken)).ToActionResult(this);
}
