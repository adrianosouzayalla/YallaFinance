namespace YallaFinance.Infrastructure.Repositories.Jobs;

public static class JobExecutionSortSql
{
    public static string Order(string? key, string direction)
    {
        if (direction is not ("asc" or "desc"))
            throw new ArgumentException("Invalid sort direction.");
        var expression = key switch
        {
            null or "started" => "started_at",
            "id" => "id",
            "finished" => "finished_at",
            "duration" => "GREATEST(0, EXTRACT(EPOCH FROM (COALESCE(finished_at, CURRENT_TIMESTAMP) - started_at)))",
            "status" => "CASE WHEN finished_at IS NULL THEN 'Em processamento' WHEN error IS NULL THEN 'Sucesso' ELSE 'Erro' END",
            "error" => "CASE WHEN error IS NULL THEN NULL WHEN split_part(error, ':', 1) IN ("
                + string.Join(
                    ", ",
                    YallaFinance.Application.Features.Jobs.Shared.Monitoring.JobMonitoringResultProjector.KnownErrorCodes
                        .Select(code => "'" + code.Replace("'", "''") + "'"))
                + ") THEN split_part(error, ':', 1) ELSE 'JobProcessingFailed' END",
            _ => throw new ArgumentException("Invalid sort key.")
        };
        return key is null ? "started_at DESC, id DESC" : $"{expression} {(direction == "asc" ? "ASC" : "DESC")} NULLS LAST, id ASC";
    }
}
