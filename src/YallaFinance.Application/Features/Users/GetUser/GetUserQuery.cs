using MediatR;
using YallaFinance.Application.Features.Auth.ChangePassword;
using YallaFinance.Application.Features.Users.CreateUser;
using YallaFinance.Application.Features.Users.ListUsers;
using YallaFinance.Application.Features.Users.ResetUserPassword;
using YallaFinance.Application.Features.Users.SetUserActive;
using YallaFinance.Application.Features.Users.UpdateUser;
using YallaFinance.Application.Services.Users.Contracts;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Users.GetUser;

public sealed record GetUserQuery(Guid Id) : IRequest<GetUserResult>;
