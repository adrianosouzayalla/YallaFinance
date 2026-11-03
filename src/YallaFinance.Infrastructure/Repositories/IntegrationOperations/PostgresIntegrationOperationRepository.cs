using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;

using YallaFinance.Domain.Integrations;
using YallaFinance.Domain.Jobs;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.IntegrationOperations;
using YallaFinance.Application.Repositories.IntegrationOperations.Contracts;

namespace YallaFinance.Infrastructure.Repositories.IntegrationOperations;

public sealed class PostgresIntegrationOperationRepository(YallaFinanceDbContext db) : IIntegrationOperationRepository
{
    public Task<IntegrationOperation?> FindAsync(
        Guid tenantId,
        string type,
        string externalId,
        CancellationToken token) =>
        new PostgresDatabaseConnection(db).ExecuteAsync<IntegrationOperation?>(
            async (connection, transaction) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                SELECT id, tenant_id, type, external_id, request_fingerprint, job_id,
                       resource_type, resource_id, created_at, completed_at
                FROM core.integration_operation WHERE tenant_id=@tenant AND type=@type AND external_id=@external
                """,
                    connection,
                    transaction);
                command.Parameters.AddWithValue("tenant", tenantId);
                command.Parameters.AddWithValue("type", type);
                command.Parameters.AddWithValue(
                    "external",
                    externalId);
                await using var reader = await command.ExecuteReaderAsync(token);
                return await reader.ReadAsync(token) ? ReadIntegrationOperation(reader) : null;
            },
            token);

    public Task<IntegrationOperationSnapshot?> GetAsync(
        Guid tenantId,
        Guid operationId,
        CancellationToken token) =>
        new PostgresDatabaseConnection(db).ExecuteAsync<IntegrationOperationSnapshot?>(
            async (connection, transaction) =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    SELECT o.id, o.tenant_id, o.type, o.external_id, o.request_fingerprint, o.job_id,
                           o.resource_type, o.resource_id, o.created_at, o.completed_at, j.status, e.error
                    FROM core.integration_operation o
                    JOIN core.job j ON j.tenant_id=o.tenant_id AND j.id=o.job_id
                    LEFT JOIN LATERAL (
                        SELECT error FROM core.job_execution
                        WHERE tenant_id=j.tenant_id AND job_id=j.id
                        ORDER BY started_at DESC, id DESC LIMIT 1
                    ) e ON true
                    WHERE o.tenant_id=@tenant AND o.id=@operation
                    """,
                    connection,
                    transaction);
                command.Parameters.AddWithValue("tenant", tenantId);
                command.Parameters.AddWithValue("operation", operationId);
                await using var reader = await command.ExecuteReaderAsync(token);
                return await reader.ReadAsync(token)
                    ? new IntegrationOperationSnapshot(
                        ReadIntegrationOperation(reader),
                        (JobStatus)reader.GetInt32(10),
                        reader.IsDBNull(11) ? null : reader.GetString(11))
                    : null;
            },
            token);

    public async Task LockAsync(Guid tenantId, string type, string externalId, CancellationToken token)
    {
        var resources = new PostgresDatabaseConnection(db);
        resources.RequireTransaction();
        await resources.ExecuteAsync(
            async (connection, transaction) =>
            {
                // JSON array avoids ambiguous separators in external identifiers.
                var key = System.Text.Json.JsonSerializer.Serialize(new[] { tenantId.ToString(), type, externalId });
                await using var command = new NpgsqlCommand(
                    "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))",
                    connection,
                    transaction);
                command.Parameters.AddWithValue("key", "integration-operation:" + key);
                return await command.ExecuteNonQueryAsync(token);
            },
            token);
    }

    public Task AddAsync(IntegrationOperation operation, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        new PostgresDatabaseConnection(db).RequireTransaction();
        // Properties and timestamp types verified against the owner's generated scaffold.
        db.IntegrationOperations.Add(new YallaFinance.Infrastructure.Database.Generated.Entities.IntegrationOperation
        {
            Id = operation.Id,
            TenantId = operation.TenantId,
            Type = operation.Type,
            ExternalId = operation.ExternalId,
            RequestFingerprint = operation.RequestFingerprint,
            JobId = operation.JobId,
            ResourceType = operation.ResourceType,
            ResourceId = operation.ResourceId,
            CreatedAt = operation.CreatedAt.UtcDateTime,
            CompletedAt = operation.CompletedAt?.UtcDateTime
        });
        return Task.CompletedTask;
    }

    private static IntegrationOperation ReadIntegrationOperation(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetGuid(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.IsDBNull(7) ? null : reader.GetGuid(7),
        reader.GetFieldValue<DateTimeOffset>(8),
        reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9));
}
