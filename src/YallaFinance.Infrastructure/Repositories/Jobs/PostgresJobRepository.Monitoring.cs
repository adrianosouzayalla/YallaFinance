using YallaFinance.Infrastructure.Utils.Database;
using System.Data;
using Npgsql;

using YallaFinance.Domain.Jobs;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed partial class PostgresJobRepository
{
    public async Task<long> CountExecutionsAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        YallaFinance.Application.Features.Jobs.Shared.JobQueryValidation.Tenant(tenantId);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(JobMonitoringSql.ExecutionCountSql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("job", jobId);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<IReadOnlyList<string>> ListTypesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        YallaFinance.Application.Features.Jobs.Shared.JobQueryValidation.Tenant(tenantId);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(JobMonitoringSql.TypesSql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        var types = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            types.Add(reader.GetString(0));
        return types;
    }

    public async Task<JobMonitoringPage<JobMonitoringRow>> ListAsync(
        Guid tenantId,
        JobMonitoringFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = JobMonitoringSql.Build(tenantId, filter, page, pageSize);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        await using var count = new NpgsqlCommand(query.CountSql, connection, transaction);
        AddJobMonitoringFilters(count, tenantId, query);
        long total;
        JobMonitoringSummary summary;
        await using (var reader = await count.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            total = reader.GetInt64(0);
            summary = new(reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
        }
        await using var command = new NpgsqlCommand(query.RowsSql, connection, transaction);
        AddJobMonitoringFilters(command, tenantId, query);
        command.Parameters.AddWithValue("limit", pageSize);
        command.Parameters.AddWithValue("offset", query.Offset);
        var rows = new List<JobMonitoringRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    (JobStatus)reader.GetInt32(3),
                    reader.GetFieldValue<DateTimeOffset>(4),
                    reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                    reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                    reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7)));
        await transaction.CommitAsync(cancellationToken);
        return new(rows, page, pageSize, total, summary);
    }

    private static void AddJobMonitoringFilters(NpgsqlCommand command, Guid tenantId, JobMonitoringSql query)
    {
        command.Parameters.AddWithValue("tenant", tenantId);
        if (query.KeyPattern is not null)
            command.Parameters.AddWithValue("key", query.KeyPattern);
        if (query.StartDate is { } start)
            command.Parameters.AddWithValue("startDate", start);
        if (query.EndDate is { } end)
            command.Parameters.AddWithValue("endDate", end);
        if (query.Type is not null)
            command.Parameters.AddWithValue("type", query.Type);
        if (query.Status is not null)
            command.Parameters.AddWithValue("status", (int)query.Status);
        if (query.TypeTitles is { } titles)
            for (var index = 0; index < titles.Count; index++)
            {
                command.Parameters.AddWithValue($"sort_type_{index}", titles[index].Key);
                command.Parameters.AddWithValue($"sort_title_{index}", titles[index].Value);
            }
    }
}
