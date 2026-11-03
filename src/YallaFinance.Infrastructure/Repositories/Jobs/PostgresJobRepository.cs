using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;
using NpgsqlTypes;
using YallaFinance.Application.Common.Context;
using YallaFinance.Infrastructure.Database.Auditing;

using YallaFinance.Domain.Jobs;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed partial class PostgresJobRepository : IJobRepository
{
    private readonly YallaFinanceDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly CurrentExecutionActorContext actorContext;

    public PostgresJobRepository(YallaFinanceDbContext db, TimeProvider timeProvider, CurrentExecutionActorContext actorContext)
    {
        this.db = db;
        this.timeProvider = timeProvider;
        this.actorContext = actorContext;
    }

    public async Task CreateAsync(Job job, CancellationToken cancellationToken)
    {
        var audit = AuditWriteMetadata.Capture(actorContext, timeProvider);
        await new PostgresDatabaseConnection(db).ExecuteAsync(
            async (connection, transaction) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                INSERT INTO core.job (id, tenant_id, type, key, payload, status, created_at, created_by, updated_at, updated_by)
                VALUES (@id, @tenant, @type, @key, @payload, @status, @auditNow, @auditActor, NULL, NULL)
                """,
                    connection,
                    transaction);
                command.Parameters.AddWithValue("id", job.Id);
                command.Parameters.AddWithValue("tenant", job.TenantId);
                command.Parameters.AddWithValue("type", job.Type);
                command.Parameters.AddWithValue("key", job.Key);
                command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, job.Payload);
                command.Parameters.AddWithValue("status", (int)job.Status);
                audit.AddParameters(command);
                return await command.ExecuteNonQueryAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<ClaimedJob?> TryClaimNextAsync(CancellationToken cancellationToken)
    {
        var audit = AuditWriteMetadata.Capture(actorContext, timeProvider);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        Job? job;
        await using (var select = new NpgsqlCommand(
            $"SELECT {JobDataReader.JobColumns} FROM core.job WHERE status = 0 ORDER BY created_at, id LIMIT 1 FOR UPDATE SKIP LOCKED",
            connection,
            transaction))
        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
            job = await reader.ReadAsync(cancellationToken) ? JobDataReader.ReadJob(reader) : null;
        if (job is null)
            return null;
        var currentUtcTime = new DateTimeOffset(audit.UtcNow);
        var execution = new JobExecution(Guid.NewGuid(), job.TenantId, job.Id, currentUtcTime);
        await using var claim = new NpgsqlCommand(
            """
            UPDATE core.job SET status = 1, updated_at = @auditNow, updated_by = @auditActor WHERE id = @job AND tenant_id = @tenant AND status = 0;
            INSERT INTO core.job_execution (id, tenant_id, job_id, started_at, created_at, created_by, updated_at, updated_by)
            VALUES (@execution, @tenant, @job, @now, @auditNow, @auditActor, NULL, NULL);
            """,
            connection,
            transaction);
        claim.Parameters.AddWithValue("now", currentUtcTime);
        audit.AddParameters(claim);
        claim.Parameters.AddWithValue("job", job.Id);
        claim.Parameters.AddWithValue("tenant", job.TenantId);
        claim.Parameters.AddWithValue("execution", execution.Id);
        await claim.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(job with
        {
            Status = JobStatus.Processing,
            UpdatedAt = currentUtcTime
        }, execution);
    }
}
