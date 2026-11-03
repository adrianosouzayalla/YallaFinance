using YallaFinance.Infrastructure.Utils.Database;
using Npgsql;

using YallaFinance.Domain.Jobs;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed partial class PostgresJobRepository
{
    public async Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        JobQueryValidation.Tenant(tenantId);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT {JobDataReader.JobColumns} FROM core.job WHERE tenant_id = @tenant AND id = @job",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("job", jobId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? JobDataReader.ReadJob(reader) : null;
    }

    public async Task<JobPage<Job>> ListAsync(
        Guid tenantId,
        JobListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var offset = JobQueryValidation.Offset(tenantId, page, pageSize);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        var where = "tenant_id = @tenant";
        if (filter.Key is not null)
            where += " AND key = @key";
        if (filter.Type is not null)
            where += " AND type = @type";
        await using var command = new NpgsqlCommand(
            $"SELECT {JobDataReader.JobColumns} FROM core.job WHERE {where} ORDER BY created_at, id LIMIT @limit OFFSET @offset",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        if (filter.Key is not null)
            command.Parameters.AddWithValue("key", filter.Key);
        if (filter.Type is not null)
            command.Parameters.AddWithValue("type", filter.Type);
        command.Parameters.AddWithValue("limit", pageSize);
        command.Parameters.AddWithValue("offset", offset);
        var items = new List<Job>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(JobDataReader.ReadJob(reader));
        return new(items, page, pageSize);
    }

    public async Task<JobPage<JobExecution>> ListExecutionsAsync(
        Guid tenantId,
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? sortBy = null,
        string sortDirection = "asc")
    {
        var offset = JobQueryValidation.Offset(tenantId, page, pageSize);
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT {JobDataReader.ExecutionColumns} FROM core.job_execution WHERE tenant_id = @tenant AND job_id = @job ORDER BY {JobExecutionSortSql.Order(
                sortBy,
                sortDirection)} LIMIT @limit OFFSET @offset",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("job", jobId);
        command.Parameters.AddWithValue("limit", pageSize);
        command.Parameters.AddWithValue("offset", offset);
        var items = new List<JobExecution>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(JobDataReader.ReadExecution(reader));
        return new(items, page, pageSize);
    }
}
