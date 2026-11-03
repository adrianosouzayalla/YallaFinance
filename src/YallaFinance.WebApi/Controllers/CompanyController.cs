using MediatR;
using Microsoft.AspNetCore.Mvc;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Features.Categories.ListCategories;
using YallaFinance.Application.Features.PaymentMethods.ListPaymentMethods;
using YallaFinance.WebApi.Authorization;
using YallaFinance.WebApi.Extensions;

namespace YallaFinance.WebApi.Controllers;

[ApiController]
[RequireFunctionality("SYSTEM_SETTINGS")]
[Route("api/company")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CompanyController(IMediator mediator) : ControllerBase
{
    [HttpGet("settings/payments")]
    [ProducesResponseType<CompanyPaymentsSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsSettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanyPaymentsSettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet("settings/session")]
    [ProducesResponseType<CompanySessionSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionSettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanySessionSettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet("settings/security")]
    [ProducesResponseType<CompanySecuritySettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecuritySettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanySecuritySettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPut("settings/security")]
    public async Task<IActionResult> SaveSecuritySettings([FromBody] SaveCompanySecuritySettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpGet("settings/user-registration")]
    [ProducesResponseType<CompanyUserRegistrationSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRegistrationSettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanyUserRegistrationSettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet("settings/financial-settlement")]
    [ProducesResponseType<CompanyFinancialSettlementSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialSettlementSettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanyFinancialSettlementSettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPut("settings/payments")]
    public async Task<IActionResult> SavePaymentsSettings([FromBody] SaveCompanyPaymentsSettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPut("settings/session")]
    public async Task<IActionResult> SaveSessionSettings([FromBody] SaveCompanySessionSettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPut("settings/user-registration")]
    public async Task<IActionResult> SaveUserRegistrationSettings([FromBody] SaveCompanyUserRegistrationSettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPut("settings/financial-settlement")]
    public async Task<IActionResult> SaveFinancialSettlementSettings([FromBody] SaveCompanyFinancialSettlementSettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpGet("settings/job-retry")]
    [ProducesResponseType<CompanyJobRetrySettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobRetrySettings(CancellationToken cancellationToken)
        => (await mediator.Send(new GetCompanyJobRetrySettingsQuery(), cancellationToken)).ToActionResult(this);

    [HttpPut("settings/job-retry")]
    public async Task<IActionResult> SaveJobRetrySettings([FromBody] SaveCompanyJobRetrySettingsCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult(this);

    [HttpGet("settings/catalogs/categories")]
    public async Task<IActionResult> ListSettingCategories(CancellationToken cancellationToken)
        => (await mediator.Send(new ListCategoriesQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet("settings/catalogs/payment-methods")]
    public async Task<IActionResult> ListSettingPaymentMethods(CancellationToken cancellationToken)
        => (await mediator.Send(new ListPaymentMethodsQuery(), cancellationToken)).ToActionResult(this);

}
