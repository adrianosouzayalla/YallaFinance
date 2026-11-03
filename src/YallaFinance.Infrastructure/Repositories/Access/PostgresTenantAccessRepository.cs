using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Services.Authentication;

namespace YallaFinance.Infrastructure.Repositories.Access;


public sealed class PostgresTenantAccessRepository(YallaFinanceDbContext db) : YallaFinance.Application.Repositories.Authentication.ITenantAccessRepository
{
    public async Task<IReadOnlyList<TenantOption>> GetAuthorizedTenantsAsync(
        string email,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(email))
            return [];
        return await Query(email).ToListAsync(cancellationToken);
    }

    public Task<bool> IsTenantAvailableAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Tenants.AsNoTracking().AnyAsync(
            tenant => tenant.Id == tenantId && tenant.Active,
            cancellationToken);

    internal IQueryable<TenantOption> Query(string email) => db.Database.SqlQuery<TenantOption>($"""
        SELECT t.id::text AS "Id", t.name AS "Name"
        FROM core.tenant_access a
        JOIN core.tenant t ON t.id = a.tenant_id
        WHERE a.user_email = {TenantAccessEmail.Normalize(email)} AND a.active AND t.active
        ORDER BY t.name, t.id
        """);
}
