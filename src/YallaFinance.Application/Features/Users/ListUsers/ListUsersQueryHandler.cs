using MediatR;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.ListUsers;

public sealed class ListUsersQueryHandler(IUserRepository service)
    : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    public async Task<ListUsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        new(await service.ListAsync(request.Filter, cancellationToken));
}
