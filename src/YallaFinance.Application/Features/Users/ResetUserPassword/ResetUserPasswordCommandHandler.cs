using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.ResetUserPassword;

public sealed class ResetUserPasswordCommandHandler(IUserRepository service)
    : IRequestHandler<ResetUserPasswordCommand>
{
    public Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) =>
        service.ResetPasswordAsync(request.Id, request.ConfirmGlobalImpact, cancellationToken);
}
