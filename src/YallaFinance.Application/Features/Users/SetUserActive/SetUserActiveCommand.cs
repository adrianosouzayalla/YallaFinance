using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.SetUserActive;

public sealed record SetUserActiveCommand(Guid Id, SetUserActiveInput Input) : IRequest;
