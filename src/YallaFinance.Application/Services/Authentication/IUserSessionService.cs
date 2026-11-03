using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Services.Authentication;

public interface IUserSessionService
{
    Task<AuthenticationResult> CreateAsync(Guid userId, TenantOption tenant,
        IReadOnlyList<TenantOption> tenants, CancellationToken ct);
    Task<SessionContext?> ValidateAsync(string token, bool allowRevoked, CancellationToken ct);
    Task RevokeAsync(SessionContext context, CancellationToken ct);
    Task<ActiveSessionPage> ListAsync(SessionContext context, ActiveSessionFilter filter, CancellationToken ct);
}
