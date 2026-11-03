using MediatR;

using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.ListSessions;

public sealed class ListSessionsQueryHandler(IUserSessionService sessions, CurrentSessionContext current)
    : IRequestHandler<ListSessionsQuery, NotificationResult<ListSessionsResult>>
{
    public async Task<NotificationResult<ListSessionsResult>> Handle(ListSessionsQuery request, CancellationToken cancellationToken)
    {
        if (current.Value is not { } context)
            return NotificationResult<ListSessionsResult>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Sessão inválida."));

        var activeSessionPage = await sessions.ListAsync(context, request.Filter, cancellationToken);
        return NotificationResult<ListSessionsResult>.Success(new(activeSessionPage));
    }
}
