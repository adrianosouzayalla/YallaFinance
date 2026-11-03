using Npgsql;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Infrastructure.Utils.Database;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed partial class PostgresJobRepository : IJobRetryRepository
{
    public async Task<IReadOnlyList<JobRetryCandidate>> ListFailedJobsForRetryAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var candidates = new List<JobRetryCandidate>();
        await using var connection = await new PostgresDatabaseConnection(db).OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"""
            SELECT {JobDataReader.JobColumns},
                (SELECT COUNT(*) FROM core.job_execution e WHERE e.job_id = core.job.id) AS attempts,
                (SELECT MAX(e.finished_at) FROM core.job_execution e WHERE e.job_id = core.job.id) AS last_finished_at
            FROM core.job
            WHERE core.job.status = 3
            ORDER BY last_finished_at NULLS FIRST
            LIMIT @batch
            """,
            connection);
        command.Parameters.AddWithValue("batch", batchSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var job = JobDataReader.ReadJob(reader);
            var attempts = (int)reader.GetInt64(8);
            var lastFinishedAt = reader.IsDBNull(9) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(9);
            candidates.Add(new JobRetryCandidate(job, attempts, lastFinishedAt));
        }

        return candidates;
    }
}
