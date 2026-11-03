using Microsoft.EntityFrameworkCore;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.Sessions;
using YallaFinance.Application.Repositories.Sessions.Contracts;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.Infrastructure.Repositories.Access;

public sealed class PostgresUserSessionRepository(YallaFinanceDbContext db, CurrentExecutionActorContext actor, TimeProvider clock) : IUserSessionRepository
{
    public async Task<SessionRecord?> CreateAsync(Guid userId, Guid tenant, Guid id,
        DateTimeOffset now, DateTimeOffset expires, CancellationToken ct)
    {
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actor, clock);
        if (audit.ActorId != userId)
            throw new InvalidOperationException("A session must be created by its validated owner.");
        // Row locks serialize supported deactivations against creation; all activity is
        // checked again in the locked SELECT. No profile or grant participates here.
        var inserted = await db.Database.SqlQuery<Guid>($"""
            WITH allowed AS MATERIALIZED (
                SELECT a.user_id, a.tenant_id
                FROM core.tenant_access a
                JOIN core."user" u ON u.id = a.user_id
                JOIN core.tenant t ON t.id = a.tenant_id
                WHERE a.tenant_id = {tenant} AND a.user_id = {userId}
                  AND a.active AND u.active AND NOT u.is_operational AND t.active
                FOR UPDATE OF u, t, a
            )
            INSERT INTO core.user_session (id, user_id, tenant_id, created_at, expires_at, created_by, updated_at, updated_by)
            SELECT {id}, user_id, tenant_id, {audit.UtcNow}, {expires.UtcDateTime}, {audit.ActorId}, NULL, NULL FROM allowed
            RETURNING user_id AS "Value"
            """).ToArrayAsync(ct);
        return inserted.Length == 0 ? null : await FindAsync(id, inserted[0], tenant, ct);
    }

    public Task<SessionRecord?> FindAsync(Guid id, Guid user, Guid tenant, CancellationToken ct)
        => LookupQuery(id, user, tenant).Select(Projection).SingleOrDefaultAsync(ct);

    public Task<SessionRecord?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.UserSessions.AsNoTracking().Where(s => s.Id == id).Select(Projection).SingleOrDefaultAsync(ct);

    private static readonly System.Linq.Expressions.Expression<Func<UserSession, SessionRecord>> Projection =
        s => new SessionRecord(s.Id, s.UserId, s.TenantId,
            s.User.Name, s.TenantAccess.UserEmail, new DateTimeOffset(s.CreatedAt), new DateTimeOffset(s.ExpiresAt),
            s.RevokedAt == null ? null : new DateTimeOffset(s.RevokedAt.Value),
            s.User.Active && !s.User.IsOperational && s.TenantAccess.Active && s.Tenant.Active, s.Tenant.Name);

    internal IQueryable<UserSession> LookupQuery(Guid id, Guid user, Guid tenant)
        => db.UserSessions.AsNoTracking().Where(s => s.Id == id && s.UserId == user && s.TenantId == tenant);

    public async Task RevokeAsync(SessionContext context, DateTimeOffset now, CancellationToken ct)
    {
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actor, clock);
        await db.UserSessions.Where(s => s.Id == context.SessionId && s.UserId == context.UserId
            && s.TenantId == context.TenantId && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, now.UtcDateTime)
                .SetProperty(v => v.UpdatedAt, audit.UtcNow).SetProperty(v => v.UpdatedBy, audit.ActorId), ct);
    }

    internal IQueryable<UserSession> ActiveQuery(Guid tenant, DateTimeOffset now) => db.UserSessions.AsNoTracking()
        .Where(s => s.TenantId == tenant && s.RevokedAt == null && s.CreatedAt <= now.UtcDateTime
            && s.ExpiresAt > now.UtcDateTime && s.User.Active && !s.User.IsOperational && s.Tenant.Active && s.TenantAccess.Active);

    public async Task<ActiveSessionPage> ListAsync(Guid tenant, Guid current, ActiveSessionFilter filter,
        DateTimeOffset now, CancellationToken ct)
    {
        filter.Validate();
        var query = ActiveQuery(tenant, now);
        var count = await query.CountAsync(ct);
        var isDescending = filter.SortDirection == "desc";
        IOrderedQueryable<UserSession> ordered = filter.SortBy switch
        {
            "name" => isDescending ? query.OrderByDescending(s => s.User.Name) : query.OrderBy(s => s.User.Name),
            "email" => isDescending ? query.OrderByDescending(s => s.TenantAccess.UserEmail) : query.OrderBy(s => s.TenantAccess.UserEmail),
            "expiresAt" => isDescending ? query.OrderByDescending(s => s.ExpiresAt) : query.OrderBy(s => s.ExpiresAt),
            _ => isDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt)
        };
        var items = await ordered.ThenByDescending(s => s.Id).Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize).Select(s => new ActiveSessionItem(s.User.Name, s.TenantAccess.UserEmail,
                new DateTimeOffset(s.CreatedAt), new DateTimeOffset(s.ExpiresAt), s.Id == current,
                CreateSessionRowKey(s.Id))).ToArrayAsync(ct);
        return new(items, count, filter.Page, filter.PageSize);
    }

    private static string CreateSessionRowKey(Guid id) => Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(id.ToByteArray()));
}
