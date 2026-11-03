using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password, string? TenantId = null) : IRequest<NotificationResult<LoginResult>>;
