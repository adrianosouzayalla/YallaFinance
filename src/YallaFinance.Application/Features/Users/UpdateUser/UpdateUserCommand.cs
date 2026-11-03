using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, UpdateUserInput Input) : IRequest<UpdateUserResult>;
