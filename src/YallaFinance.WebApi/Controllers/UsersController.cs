using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using YallaFinance.Application.Features.Users.ListUsers;
using YallaFinance.Application.Features.Users.GetUser;
using YallaFinance.Application.Features.Users.CreateUser;
using YallaFinance.Application.Features.Users.UpdateUser;
using YallaFinance.Application.Features.Users.SetUserActive;
using YallaFinance.Application.Features.Users.ResetUserPassword;
using YallaFinance.Application.Features.Users.GetInitialPassword;
using YallaFinance.WebApi.Authentication;
using YallaFinance.WebApi.Authorization;
namespace YallaFinance.WebApi.Controllers;
[ApiController, RequireFunctionality("USERS"), Route("api/users")]
[TypeFilter(typeof(UserManagementExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserFilter filter, CancellationToken ct) =>
        Ok((await mediator.Send(new ListUsersQuery(filter), ct)).Page);
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok((await mediator.Send(new GetUserQuery(id), ct)).User);
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserInput input, CancellationToken ct)
    {
        var user = (await mediator.Send(new CreateUserCommand(input), ct)).User;
        return Created($"/api/users/{user.Id}", user);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserInput input, CancellationToken ct) =>
        Ok((await mediator.Send(new UpdateUserCommand(id, input), ct)).User);
    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> Active(Guid id, SetUserActiveInput input, CancellationToken ct)
    { await mediator.Send(new SetUserActiveCommand(id, input), ct); return NoContent(); }
    [HttpPost("{id:guid}/password")]
    public async Task<IActionResult> Password(Guid id, ResetUserPasswordInput input, CancellationToken ct)
    { await mediator.Send(new ResetUserPasswordCommand(id, input.ConfirmGlobalImpact), ct); return NoContent(); }
    [HttpGet("initial-password")]
    public async Task<IActionResult> InitialPassword(CancellationToken ct) =>
        Ok(await mediator.Send(new GetInitialPasswordQuery(), ct));
}

