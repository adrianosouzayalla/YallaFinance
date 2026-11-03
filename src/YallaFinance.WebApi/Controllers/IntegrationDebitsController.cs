using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.WebApi.Binding;
using YallaFinance.WebApi.Contracts;
using YallaFinance.WebApi.Extensions;
namespace YallaFinance.WebApi.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Route("api/integrations/debits")]
public sealed class IntegrationDebitsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(InboundOperationReceiptResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(InboundOperationReceiptResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Receive(
        [InboundDebitBody] ReceiveDebitPayload payload,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReceiveInboundDebitCommand(payload), cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(this);
        return StatusCode(result.Value!.Created ? StatusCodes.Status202Accepted : StatusCodes.Status200OK,
            new InboundOperationReceiptResponse(result.Value.OperationId));
    }
}
