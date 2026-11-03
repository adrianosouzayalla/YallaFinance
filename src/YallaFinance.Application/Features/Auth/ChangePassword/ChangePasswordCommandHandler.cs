using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(IUserRepository users)
    : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        users.ChangeOwnPasswordAsync(request.Input, cancellationToken);
}
