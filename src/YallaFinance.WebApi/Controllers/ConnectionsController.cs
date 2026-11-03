using MediatR;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Connections.Providers.Nibo;
using YallaFinance.Application.Features.Connections.Providers.Sicoob;
using YallaFinance.Application.Features.Connections.Shared;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[RequireFunctionality("SYSTEM_SETTINGS")]
[Route("api/company/connections")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ConnectionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CompanyPage<CompanyConnectionSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConnections([FromQuery] CompanyConnectionFilter filter, CancellationToken cancellationToken)
        => (await mediator.Send(new ListCompanyConnectionsQuery(filter), cancellationToken)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CompanyConnectionDetail>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnection(Guid id, [FromQuery] CompanyConnectionDetailFilter filter, CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanyConnectionQuery(id, filter), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> CreateConnection([FromBody] SaveCompanyConnectionCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = null }, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateConnection(Guid id, [FromBody] SaveCompanyConnectionCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = id }, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}/settings")]
    public async Task<IActionResult> SaveConnectionSettings(Guid id, [FromBody] IReadOnlyList<CompanyConnectionSettingInput> settings, CancellationToken cancellationToken)
        => (await mediator.Send(new SaveCompanyConnectionSettingsCommand(id, settings), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/mappings/{mappingType}")]
    public async Task<IActionResult> CreateConnectionMapping(Guid id, string mappingType, [FromBody] SaveCompanyConnectionMappingCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = id, MappingId = null, MappingType = mappingType }, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}/mappings/{mappingType}/{mappingId:guid}")]
    public async Task<IActionResult> UpdateConnectionMapping(Guid id, string mappingType, Guid mappingId,
        [FromBody] SaveCompanyConnectionMappingCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = id, MappingId = mappingId, MappingType = mappingType }, cancellationToken)).ToActionResult(this);

    [HttpDelete("{id:guid}/mappings/{mappingId:guid}")]
    public async Task<IActionResult> RemoveConnectionMapping(Guid id, Guid mappingId, CancellationToken cancellationToken)
        => (await mediator.Send(new RemoveCompanyConnectionMappingCommand(id, mappingId), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/payment-methods")]
    public async Task<IActionResult> CreatePaymentMethodConnection(Guid id, [FromBody] SaveCompanyPaymentMethodConnectionCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = id, LinkId = null }, cancellationToken)).ToActionResult(this);

    [HttpPut("{id:guid}/payment-methods/{linkId:guid}")]
    public async Task<IActionResult> UpdatePaymentMethodConnection(Guid id, Guid linkId,
        [FromBody] SaveCompanyPaymentMethodConnectionCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command with { ConnectionId = id, LinkId = linkId }, cancellationToken)).ToActionResult(this);

    [HttpDelete("{id:guid}/payment-methods/{linkId:guid}")]
    public async Task<IActionResult> RemovePaymentMethodConnection(Guid id, Guid linkId, CancellationToken cancellationToken)
        => (await mediator.Send(new RemoveCompanyPaymentMethodConnectionCommand(id, linkId), cancellationToken)).ToActionResult(this);
}
