using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed partial class PostgresJobRepository
{
    public async Task<bool> TryRequeueAsync(
        Guid tenantId,
        Guid jobId,
        DateTimeOffset? expectedUpdatedAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        JobQueryValidation.Tenant(tenantId);
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, timeProvider);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(JobReprocessingSql.Requeue, connection, transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("job", jobId);
        command.Parameters.AddWithValue("expectedUpdatedAt", NpgsqlTypes.NpgsqlDbType.TimestampTz,
            (object?)expectedUpdatedAt?.UtcDateTime ?? DBNull.Value);
        audit.AddParameters(command);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            return false;
        await using var reset = new NpgsqlCommand(
            """
            UPDATE core.integration_operation SET completed_at=NULL, resource_type=NULL, resource_id=NULL,
                updated_at=@auditNow, updated_by=@auditActor
            WHERE tenant_id=@tenant AND job_id=@job
              AND (completed_at IS NOT NULL OR resource_type IS NOT NULL OR resource_id IS NOT NULL)
            """,
            connection,
            transaction);
        reset.Parameters.AddWithValue("tenant", tenantId);
        reset.Parameters.AddWithValue("job", jobId);
        audit.AddParameters(reset);
        await reset.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
