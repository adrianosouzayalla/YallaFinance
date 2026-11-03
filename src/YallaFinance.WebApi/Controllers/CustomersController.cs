using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Customers.FindMovements;
using YallaFinance.Application.Features.Customers.SearchCustomers;
using YallaFinance.Application.Features.Customers.SuggestCustomers;
using YallaFinance.Application.Features.Customers.ListCustomerContracts;
using YallaFinance.WebApi.Extensions;
using YallaFinance.WebApi.Authorization;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet("suggestions"), RequireFunctionality("DEBTS", "DEBTS_CREATE", "CHARGES", "CHARGES_CREATE")]
    public async Task<IActionResult> Suggestions(
        [FromQuery] string search = "",
        [FromQuery] string? source = null,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(new SuggestCustomersQuery(search, source), cancellationToken)).ToActionResult(this);

    [HttpGet("{customerId}/contracts"), RequireFunctionality("DEBTS", "DEBTS_CREATE", "CHARGES", "CHARGES_CREATE")]
    public async Task<IActionResult> Contracts(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? occurrenceDate = null,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(
            new ListCustomerContractsQuery(customerId, page, pageSize, occurrenceDate),
            cancellationToken)).ToActionResult(this);

    [HttpGet, RequireFunctionality("DEBTS", "DEBTS_CREATE", "CHARGES", "CHARGES_CREATE")]
    public async Task<IActionResult> Search(
        [FromQuery] string search = "",
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(new SearchCustomersQuery(search), cancellationToken)).ToActionResult(this);

    [HttpGet("{customerId}/movements"), RequireFunctionality("DEBTS", "DEBTS_CREATE", "CHARGES", "CHARGES_CREATE")]
    public async Task<IActionResult> Movements(
        string customerId,
        [FromQuery] DateOnly occurrenceDate,
        CancellationToken cancellationToken) =>
        (await mediator.Send(new FindMovementsQuery(customerId, occurrenceDate), cancellationToken)).ToActionResult(this);
}
