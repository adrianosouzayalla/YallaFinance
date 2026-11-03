using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.SetUserActive;

public sealed class SetUserActiveCommandHandler(IUserRepository service)
    : IRequestHandler<SetUserActiveCommand>
{
    public Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken) =>
        service.SetUserActiveAsync(request.Id, request.Input, cancellationToken);
}
