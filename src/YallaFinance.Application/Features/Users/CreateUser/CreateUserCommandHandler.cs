using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.CreateUser;

public sealed class CreateUserCommandHandler(IUserRepository service)
    : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        new(await service.CreateAsync(request.Input, cancellationToken));
}
