using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using YallaFinance.Application.Features.Payments.ListPaymentCharges;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.StartPublicPaymentAttempt;
using YallaFinance.Application.Features.Payments.GetPaymentRequest;
using YallaFinance.Application.Features.Payments.GetPublicCheckout;
using YallaFinance.Application.Features.Payments.RecoverPaymentLink;
using YallaFinance.Application.Features.Payments.CancelPaymentRequest;
using YallaFinance.WebApi.Payments;
using YallaFinance.WebApi.Extensions;
using YallaFinance.WebApi.Authorization;

namespace YallaFinance.WebApi.Controllers;

[ApiController, RequireFunctionality("PAYMENTS"), Route("api/payments")]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    [HttpGet("charges")]
    public async Task<IActionResult> List([FromQuery] PaymentChargesQuery query, CancellationToken ct) =>
        (await mediator.Send(query, ct)).ToActionResult(this);

    [HttpPost("prepare")]
    public async Task<IActionResult> Prepare([FromBody] Guid[] chargeIds, CancellationToken ct) =>
        (await mediator.Send(new PreparePaymentsQuery(chargeIds), ct)).ToActionResult(this);

    [HttpPost("payment-requests/batch")]
    public async Task<IActionResult> Generate(GeneratePaymentRequestsData data, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return (await mediator.Send(new GeneratePaymentRequestsCommand(data), ct)).ToActionResult(this);
    }

    [HttpGet("payment-requests/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenant))
            return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        return (await mediator.Send(new GetPaymentRequestQuery(tenant, id), ct)).Response.ToActionResult(this);
    }

    [AllowAnonymous, HttpGet("{token}")]
    public async Task<IActionResult> GetCheckout(string token, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return (await mediator.Send(new GetPublicCheckoutQuery(token), ct)).Response.ToActionResult(this);
    }

    [AllowAnonymous, HttpPost("{token}/attempts")]
    public async Task<IActionResult> StartAttempt(
        string token,
        [FromBody] StartPublicPaymentAttemptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return (await mediator.Send(new StartPublicPaymentAttemptCommand(token, request.MethodCode,
            idempotencyKey ?? string.Empty) { Card = request.Card }, ct)).ToActionResult(this);
    }

    [HttpGet("payment-requests/{id:guid}/link")]
    public async Task<IActionResult> Link(Guid id, CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenant))
            return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        return (await mediator.Send(new RecoverPaymentLinkQuery(tenant, id), ct)).Response.ToActionResult(this);
    }

    [HttpPost("payment-requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenant) || !Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) || userId == Guid.Empty)
            return Unauthorized();
        return (await mediator.Send(new CancelPaymentRequestCommand(tenant, id), ct)).Response.ToActionResult(this);
    }

    private bool TryGetTenantId(out Guid id) => Guid.TryParse(User.FindFirstValue("tenantId"), out id) &&
        id != Guid.Empty;
}
