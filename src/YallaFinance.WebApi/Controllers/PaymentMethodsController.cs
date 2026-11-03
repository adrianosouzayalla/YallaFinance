using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.PaymentMethods.ListPaymentMethods;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("api/payment-methods")]
public sealed class PaymentMethodsController(IMediator mediator) : ControllerBase
{
    [HttpGet, RequireFunctionality("CHARGES", "CHARGES_CREATE")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await mediator.Send(new ListPaymentMethodsQuery(), cancellationToken)).ToActionResult(this);
}
