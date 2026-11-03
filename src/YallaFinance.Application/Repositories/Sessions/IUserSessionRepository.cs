using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Sessions;

public interface IUserSessionRepository
{
    Task<SessionRecord?> CreateAsync(Guid userId, Guid tenant, Guid id,
        DateTimeOffset now, DateTimeOffset expires, CancellationToken ct);
    Task<SessionRecord?> FindAsync(Guid id, Guid user, Guid tenant, CancellationToken ct);
    Task<SessionRecord?> FindByIdAsync(Guid id, CancellationToken ct);
    Task RevokeAsync(SessionContext context, DateTimeOffset now, CancellationToken ct);
    Task<ActiveSessionPage> ListAsync(Guid tenant, Guid current, ActiveSessionFilter filter,
        DateTimeOffset now, CancellationToken ct);
}
