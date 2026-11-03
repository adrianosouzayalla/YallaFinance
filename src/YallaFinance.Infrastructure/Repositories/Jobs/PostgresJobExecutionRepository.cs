using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;
using NpgsqlTypes;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Features.Jobs.Processing;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed class PostgresJobExecutionRepository(YallaFinanceDbContext db, TimeProvider timeProvider,
    YallaFinance.Application.Common.Context.CurrentExecutionActorContext actorContext) : IJobExecutionRepository
{
    public async Task CompleteAsync(
        Guid tenantId,
        Guid jobId,
        Guid executionId,
        JobHandlerResult result,
        CancellationToken cancellationToken)
    {
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, timeProvider);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        // The existing Job lock also protects the optional public operation during finalization.
        bool linked;
        string jobType;
        await using (var lookup = new NpgsqlCommand(
            """
            SELECT j.type, o.id FROM core.job j
            LEFT JOIN core.integration_operation o ON o.tenant_id=j.tenant_id AND o.job_id=j.id
            WHERE j.tenant_id=@tenant AND j.id=@job FOR UPDATE OF j
            """,
            connection,
            transaction))
        {
            lookup.Parameters.AddWithValue("tenant", tenantId);
            lookup.Parameters.AddWithValue("job", jobId);
            await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Job not found for completion.");
            jobType = reader.GetString(0);
            linked = !reader.IsDBNull(1);
        }
        if (linked)
            result = InboundOperationCompletion.Normalize(jobType, result);
        // Lock Job first, consistently with claim. Validate tenant, state and open attempt together.
        await using var updateJob = new NpgsqlCommand(
            """
            UPDATE core.job j SET status = @status, updated_at = @auditNow, updated_by = @auditActor
            WHERE j.id = @job AND j.tenant_id = @tenant AND j.status = 1
              AND EXISTS (SELECT 1 FROM core.job_execution e WHERE e.id = @execution
                          AND e.job_id = j.id AND e.tenant_id = j.tenant_id AND e.finished_at IS NULL)
            """,
            connection,
            transaction);
        var currentUtcTime = new DateTimeOffset(audit.UtcNow);
        audit.AddParameters(updateJob);
        updateJob.Parameters.AddWithValue("status", result.Success ? 2 : 3);
        updateJob.Parameters.AddWithValue("now", currentUtcTime);
        updateJob.Parameters.AddWithValue("job", jobId);
        updateJob.Parameters.AddWithValue("tenant", tenantId);
        updateJob.Parameters.AddWithValue("execution", executionId);
        if (await updateJob.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("No matching open job execution can be completed.");
        await using var finish = new NpgsqlCommand(
            """
            UPDATE core.job_execution SET finished_at = GREATEST(@now, started_at), result = @result, error = @error, details = @details,
                updated_at = @auditNow, updated_by = @auditActor
            WHERE id = @execution AND tenant_id = @tenant AND job_id = @job AND finished_at IS NULL
            """,
            connection,
            transaction);
        finish.Parameters.AddWithValue("now", currentUtcTime);
        audit.AddParameters(finish);
        finish.Parameters.AddWithValue("execution", executionId);
        finish.Parameters.AddWithValue("tenant", tenantId);
        finish.Parameters.AddWithValue("job", jobId);
        finish.Parameters.AddWithValue(
            "result",
            NpgsqlDbType.Jsonb,
            (object?)result.Result?.GetRawText() ?? DBNull.Value);
        finish.Parameters.AddWithValue(
            "details",
            NpgsqlDbType.Jsonb,
            (object?)result.Details?.GetRawText() ?? DBNull.Value);
        var error = result.Success ? null : result.Error is null ? "JobHandlerError: Handler returned no error." : $"{result.Error.Code}: {result.Error.Message}";
        if (error is not null)
        {
            error = new string(error.Where(c => !char.IsControl(c)).ToArray());
            if (error.Length > 1000)
                error = error[..1000];
        }
        finish.Parameters.AddWithValue("error", NpgsqlDbType.Varchar, (object?)error ?? DBNull.Value);
        if (await finish.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("The job execution is no longer open.");
        if (linked)
        {
            await using var completeOperation = new NpgsqlCommand(
                """
                UPDATE core.integration_operation SET
                    completed_at=GREATEST(@now, created_at), resource_type=@resourceType, resource_id=@resourceId,
                    updated_at=@auditNow, updated_by=@auditActor
                WHERE tenant_id=@tenant AND job_id=@job
                """,
                connection,
                transaction);
            completeOperation.Parameters.AddWithValue("now", currentUtcTime);
            audit.AddParameters(completeOperation);
            completeOperation.Parameters.AddWithValue("tenant", tenantId);
            completeOperation.Parameters.AddWithValue(
                "job",
                jobId);
            completeOperation.Parameters.AddWithValue(
                "resourceType",
                NpgsqlDbType.Varchar,
                result.Success ? "Debit" : DBNull.Value);
            completeOperation.Parameters.AddWithValue(
                "resourceId",
                NpgsqlDbType.Uuid,
                result.Success ? InboundOperationCompletion.DebtId(result)!.Value : DBNull.Value);
            if (await completeOperation.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("The linked operation could not be completed.");
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
