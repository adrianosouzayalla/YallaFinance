using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Auth.Login;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Integrations.Auth;

public sealed record IntegrationTokenCommand(string Email, string Password, string? TenantId = null)
    : IRequest<NotificationResult<LoginResult>>;
