using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.ListUsers;

public sealed record ListUsersQuery(UserFilter Filter) : IRequest<ListUsersResult>;
