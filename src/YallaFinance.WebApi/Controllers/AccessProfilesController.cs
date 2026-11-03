using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


using YallaFinance.Application.Features.AccessProfiles.ListFunctionalities;
using YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;
using YallaFinance.Application.Features.AccessProfiles.GetAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.SetAccessProfileActive;
using YallaFinance.WebApi.Authentication;
using YallaFinance.WebApi.Authorization;
using YallaFinance.Application.Features.Users.SetUserActive;
namespace YallaFinance.WebApi.Controllers;
[ApiController, RequireFunctionality("PROFILES"), Route("api/access-profiles")]
[TypeFilter(typeof(UserManagementExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccessProfilesController(IMediator mediator) : ControllerBase
{
    [HttpGet("functionalities")]
    public async Task<IActionResult> Functionalities(CancellationToken ct) =>
        Ok((await mediator.Send(new ListFunctionalitiesQuery(), ct)).Functionalities);
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProfileFilter filter, CancellationToken ct) =>
        Ok((await mediator.Send(new ListAccessProfilesQuery(filter), ct)).Page);
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok((await mediator.Send(new GetAccessProfileQuery(id), ct)).Profile);
    [HttpPost]
    public async Task<IActionResult> Create(CreateProfileInput input, CancellationToken ct)
    {
        var profile = (await mediator.Send(new CreateAccessProfileCommand(input), ct)).Profile;
        return Created($"/api/access-profiles/{profile.Id}", profile);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProfileInput input, CancellationToken ct) =>
        Ok((await mediator.Send(new UpdateAccessProfileCommand(id, input), ct)).Profile);
    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> Active(Guid id, SetUserActiveInput input, CancellationToken ct)
    { await mediator.Send(new SetAccessProfileActiveCommand(id, input.Active), ct); return NoContent(); }
}
