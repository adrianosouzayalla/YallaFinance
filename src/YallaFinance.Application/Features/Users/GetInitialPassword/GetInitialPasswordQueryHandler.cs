using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.GetInitialPassword;

public sealed class GetInitialPasswordQueryHandler(IUserRepository service)
    : IRequestHandler<GetInitialPasswordQuery, GetInitialPasswordResult>
{
    public async Task<GetInitialPasswordResult> Handle(GetInitialPasswordQuery request, CancellationToken cancellationToken) =>
        new(await service.GetInitialPasswordAsync(cancellationToken));
}
