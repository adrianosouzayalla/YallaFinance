using MediatR;
using YallaFinance.Application.Features.Auth.ChangePassword;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(PasswordInput Input) : IRequest;
