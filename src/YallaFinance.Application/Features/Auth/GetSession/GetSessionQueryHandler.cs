using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Auth.GetSession;

public sealed class GetSessionQueryHandler(YallaFinance.Application.Common.Context.CurrentSessionContext current,
    YallaFinance.Application.Repositories.Authorization.IFunctionalityAccessRepository access)
    : MediatR.IRequestHandler<GetSessionQuery, NotificationResult<GetSessionResult>>
{
    public async Task<NotificationResult<GetSessionResult>> Handle(
        GetSessionQuery request,
        CancellationToken cancellationToken)
    {
        if (current.Value is not { } session)
            return NotificationResult<GetSessionResult>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Sessão inválida."));
        var grantedFunctionalityCodes = await access.ListGrantedCodesAsync(session, cancellationToken);
        return NotificationResult<GetSessionResult>.Success(new GetSessionResult(true, session.DisplayName,
                session.ExpiresAt.ToString("O"), session.TenantId.ToString(), session.SessionId.ToString(), session.Organization,
                session.UserId.ToString(), grantedFunctionalityCodes));
    }
}
