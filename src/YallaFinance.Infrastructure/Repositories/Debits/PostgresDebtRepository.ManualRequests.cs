using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Debits.Contracts;
namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed partial class PostgresDebtRepository
{
    public Task<ManualDebtRequest?> FindManualRequestAsync(Guid tenantId, Guid key, CancellationToken token) =>
        new PostgresDatabaseConnection(db).ExecuteAsync<ManualDebtRequest?>(
            async (connection, transaction) =>
            {
                await using var command = new NpgsqlCommand(
                    "SELECT request_fingerprint,debt_id,created_at FROM finance.manual_debt_request WHERE tenant_id=@tenant AND idempotency_key=@key",
                    connection,
                    transaction);
                command.Parameters.AddWithValue("tenant", tenantId);
                command.Parameters.AddWithValue("key", key);
                await using var reader = await command.ExecuteReaderAsync(token);
                return await reader.ReadAsync(token) ? new(
                    tenantId,
                    key,
                    reader.GetString(0),
                    reader.GetGuid(1),
                    reader.GetFieldValue<DateTimeOffset>(2)) : null;
            },
            token);

    public async Task LockManualRequestAsync(Guid tenantId, Guid key, CancellationToken token)
    {
        var resources = new PostgresDatabaseConnection(db);
        resources.RequireTransaction();
        await resources.ExecuteAsync(
            async (connection, transaction) =>
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_advisory_xact_lock(hashtextextended(@key,0))",
                    connection,
                    transaction);
                command.Parameters.AddWithValue("key", $"manual-debt-request:{tenantId}:{key}");
                return await command.ExecuteNonQueryAsync(token);
            },
            token);
    }

    public async Task AddManualRequestAsync(ManualDebtRequest receipt, CancellationToken token)
    {
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, clock);
        var resources = new PostgresDatabaseConnection(db);
        resources.RequireTransaction();
        try
        {
            await resources.ExecuteAsync(
                async (connection, transaction) =>
                {
                    await using var command = new NpgsqlCommand(
                        "INSERT INTO finance.manual_debt_request(tenant_id,idempotency_key,request_fingerprint,debt_id,created_at,created_by,updated_at,updated_by) VALUES(@tenant,@key,@fingerprint,@debt,@auditNow,@auditActor,NULL,NULL)",
                        connection,
                        transaction);
                    command.Parameters.AddWithValue("tenant", receipt.TenantId);
                    command.Parameters.AddWithValue(
                        "key",
                        receipt.IdempotencyKey);
                    command.Parameters.AddWithValue("fingerprint", receipt.RequestFingerprint);
                    command.Parameters.AddWithValue(
                        "debt",
                        receipt.DebtId);
                    audit.AddParameters(command);
                    return await command.ExecuteNonQueryAsync(token);
                },
                token);
        }
        catch (PostgresException error) when (ManualDebtRequestConstraint.IsIdentityConflict(error))
        {
            throw new ManualDebtRequestConcurrencyException(error);
        }
    }
}
