using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.FinancialCatalog;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/financial-catalog")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FinancialCatalogController(IMediator mediator) : ControllerBase
{
    [HttpPost("customers/lookup"), RequireFunctionality("CHARGES_CREATE", "DIRECT_CHARGES_CREATE")]
    public async Task<IActionResult> FindCustomersByDocument(
        [FromBody] FindFinancialCustomersByDocumentQuery request, CancellationToken cancellationToken)
        => (await mediator.Send(request, cancellationToken)).ToActionResult(this);

    [HttpGet("categories"), RequireFunctionality("CHARGES_CREATE")]
    public async Task<IActionResult> ListCategories(
        [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
        => (await mediator.Send(new ListFinancialCategoriesQuery(skip, take), cancellationToken)).ToActionResult(this);

    [HttpGet("cost-centers"), RequireFunctionality("CHARGES_CREATE", "DIRECT_CHARGES_CREATE")]
    public async Task<IActionResult> ListCostCenters(
        [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
        => (await mediator.Send(new ListFinancialCostCentersQuery(skip, take), cancellationToken)).ToActionResult(this);
}
