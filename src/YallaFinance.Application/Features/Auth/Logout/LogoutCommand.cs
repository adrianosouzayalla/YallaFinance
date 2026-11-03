using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.Logout;

public sealed record LogoutCommand : IRequest<NotificationResult<LogoutResult>>;
