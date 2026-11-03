using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.UpdateUser;

public sealed class UpdateUserCommandHandler(IUserRepository service)
    : IRequestHandler<UpdateUserCommand, UpdateUserResult>
{
    public async Task<UpdateUserResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        new(await service.UpdateAsync(request.Id, request.Input, cancellationToken));
}
