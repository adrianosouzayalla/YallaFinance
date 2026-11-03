using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.GetUser;

public sealed class GetUserQueryHandler(IUserRepository service)
    : IRequestHandler<GetUserQuery, GetUserResult>
{
    public async Task<GetUserResult> Handle(GetUserQuery request, CancellationToken cancellationToken) =>
        new(await service.GetAsync(request.Id, cancellationToken));
}
