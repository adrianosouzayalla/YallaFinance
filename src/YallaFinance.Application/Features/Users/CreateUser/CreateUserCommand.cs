using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(CreateUserInput Input) : IRequest<CreateUserResult>;
