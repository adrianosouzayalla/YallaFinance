using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler(YallaFinance.Application.Services.Authentication.IUserSessionService sessions,
    YallaFinance.Application.Common.Context.CurrentSessionContext current)
    : MediatR.IRequestHandler<LogoutCommand, NotificationResult<LogoutResult>>
{
    public async Task<NotificationResult<LogoutResult>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        if (current.Value is not { } context)
            return NotificationResult<LogoutResult>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Sessão inválida."));
        await sessions.RevokeAsync(context, cancellationToken);
        return NotificationResult<LogoutResult>.Success(new LogoutResult(true));
    }
}
