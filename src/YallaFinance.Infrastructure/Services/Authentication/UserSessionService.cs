using System.Data.Common;
using YallaFinance.Application.Repositories.Sessions;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application.Services.Authentication.Errors;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Repositories.Settings;



namespace YallaFinance.Infrastructure.Services.Authentication;

public sealed class UserSessionService(IUserSessionRepository repository,
    ITenantSettingRepository settings, TimeProvider clock, CurrentExecutionActorContext actor) : IUserSessionService
{
    public async Task<AuthenticationResult> CreateAsync(Guid userId, TenantOption tenant,
        IReadOnlyList<TenantOption> tenants, CancellationToken ct)
    {
        if (!Guid.TryParse(tenant.Id, out var tenantId)) return new(false, null, null, "TENANT_NOT_ALLOWED");
        var configuration = SessionSettings.Read(await Persist(() => settings.ReadGroupAsync(tenantId, "Authentication", ct)));
        var now = clock.GetUtcNow();
        var (token, id) = OpaqueSessionToken.Create();
        actor.SetAuthenticatedUser(userId);
        var record = await Persist(() => repository.CreateAsync(userId, tenantId, id, now,
            now.AddMinutes(configuration.LifetimeMinutes), ct));
        return record is null ? new(false, null, null, "TENANT_NOT_ALLOWED") :
            new(true, token, record.DisplayName, null, record.ExpiresAt, tenant.Id, tenants);
    }
    public async Task<SessionContext?> ValidateAsync(string token, bool allowRevoked, CancellationToken ct)
    {
        if (!OpaqueSessionToken.TryGetId(token, out var id)) return null;
        var record = await Persist(() => repository.FindByIdAsync(id, ct));
        var now = clock.GetUtcNow();
        return record is { Active: true } && record.CreatedAt <= now && record.ExpiresAt > now
            && (allowRevoked || record.RevokedAt is null) ? CreateSessionContext(record) : null;
    }
    public async Task RevokeAsync(SessionContext context, CancellationToken ct)
        => await Persist(async () => { await repository.RevokeAsync(context, clock.GetUtcNow(), ct); return true; });
    public Task<ActiveSessionPage> ListAsync(SessionContext context, ActiveSessionFilter filter, CancellationToken ct)
    {
        filter.Validate();
        return Persist(() => repository.ListAsync(context.TenantId, context.SessionId, filter, clock.GetUtcNow(), ct));
    }
    private static SessionContext CreateSessionContext(SessionRecord r) =>
        new(r.Id, r.UserId, r.TenantId, r.DisplayName, r.Email, r.ExpiresAt, r.Organization);
    private static async Task<T> Persist<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception error) when (error is DbException or TimeoutException) { throw new SessionUnavailableException(); }
    }
}
