using Npgsql;
using YallaFinance.Domain.Jobs;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

internal static class JobDataReader
{
    public const string JobColumns = "id, tenant_id, type, key, payload::text, status, created_at, updated_at";
    public const string ExecutionColumns = "id, tenant_id, job_id, started_at, finished_at, result::text, error, details::text";

    public static Job ReadJob(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        (JobStatus)reader.GetInt32(5),
        reader.GetFieldValue<DateTimeOffset>(6),
        reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7));

    public static JobExecution ReadExecution(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetGuid(2),
        reader.GetFieldValue<DateTimeOffset>(3),
        reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.IsDBNull(7) ? null : reader.GetString(7));
}
