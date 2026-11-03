using YallaFinance.Infrastructure.Database.Transactions;
using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Settings;
using YallaFinance.Application.Common.Context;
using YallaFinance.Infrastructure.Database.Auditing;

namespace YallaFinance.Infrastructure.Repositories.Settings;

public sealed class PostgresTenantSettingRepository(
    YallaFinanceDbContext db,
    IUnitOfWork unit,
    TimeProvider clock, CurrentExecutionActorContext actorContext)
    : ITenantSettingRepository
{
    public async Task<IReadOnlyDictionary<string, string>> ReadGroupAsync(
        Guid tenant,
        string group,
        CancellationToken ct)
    {
        if (tenant == Guid.Empty)
            throw new ArgumentException("Tenant is required.", nameof(tenant));
        var rows = await db.Database.SqlQuery<TenantSettingRow>($"SELECT \"key\" AS \"Key\", value AS \"Value\" FROM core.tenant_setting WHERE tenant_id = {tenant} AND \"group\" = {group}")
            .ToArrayAsync(ct);
        return rows.ToDictionary(row => row.Key, row => row.Value, StringComparer.Ordinal);
    }

    public async Task SaveAsync(
        Guid tenant,
        string group,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken ct)
    {
        if (tenant == Guid.Empty || values.Count == 0
            || !IsValidSettingName(group) || values.Any(pair => !IsValidSettingName(pair.Key) || pair.Value?.Length > 8000))
            throw new ArgumentException("Invalid tenant settings.");
        var audit = AuditWriteMetadata.Capture(actorContext, clock);
        await unit.ExecuteAsync(
            async token =>
            {
                var lockScope = $"tenant-settings:{tenant}:{group}";
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({lockScope}, 0))",
                    token);
                foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    if (pair.Value is null)
                        await db.Database.ExecuteSqlInterpolatedAsync(
                            $"DELETE FROM core.tenant_setting WHERE tenant_id = {tenant} AND \"group\" = {group} AND \"key\" = {pair.Key}",
                            token);
                    else
                    {
                        var tenantSettingId = Guid.NewGuid();
                        await db.Database.ExecuteSqlInterpolatedAsync(
                            $"INSERT INTO core.tenant_setting (id, tenant_id, \"group\", \"key\", value, created_at, updated_at, created_by, updated_by) VALUES ({tenantSettingId}, {tenant}, {group}, {pair.Key}, {pair.Value}, {audit.UtcNow}, NULL, {audit.ActorId}, NULL) ON CONFLICT (tenant_id, \"group\", \"key\") DO UPDATE SET value = EXCLUDED.value, updated_at = {audit.UtcNow}, updated_by = {audit.ActorId} WHERE core.tenant_setting.value IS DISTINCT FROM EXCLUDED.value",
                            token);
                    }
                }
                PostgresUnitOfWork.GetCommitValueOrThrow(await unit.CommitAsync(token));
                return true;
            },
            ct);
    }

    private static bool IsValidSettingName(string name) => System.Text.RegularExpressions.Regex.IsMatch(
        name,
        @"\A[A-Za-z][A-Za-z0-9_.-]{0,99}\z");
}
