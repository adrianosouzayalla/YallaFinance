
using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
namespace YallaFinance.Infrastructure.Repositories.Jobs;

public sealed record JobMonitoringSql(
    string Where,
    string SummaryWhere,
    string? KeyPattern,
    string? Type,
    JobStatus? Status,
    int Offset,
    string Order = "j.created_at DESC, j.id DESC",
    IReadOnlyList<KeyValuePair<string, string>>? TypeTitles = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null)
{
    public const string ExecutionCountSql = "SELECT count(*) FROM core.job_execution WHERE tenant_id = @tenant AND job_id = @job";
    public const string TypesSql = "SELECT DISTINCT type FROM core.job WHERE tenant_id = @tenant ORDER BY type";
    public string CountSql => $"""
        SELECT {(Status is null ? "count(*)" : "count(*) FILTER (WHERE j.status = @status)")},
            count(*) FILTER (WHERE j.status = {(int)JobStatus.Pending}),
            count(*) FILTER (WHERE j.status = {(int)JobStatus.Processing}),
            count(*) FILTER (WHERE j.status = {(int)JobStatus.Success}),
            count(*) FILTER (WHERE j.status = {(int)JobStatus.Failed})
        FROM core.job j WHERE {SummaryWhere}
        """;
    public string RowsSql => $"""
        SELECT j.id, j.type, j.key, j.status, j.created_at, j.updated_at, e.started_at, e.finished_at
        FROM core.job j
        LEFT JOIN LATERAL (
            SELECT started_at, finished_at FROM core.job_execution
            WHERE tenant_id = j.tenant_id AND job_id = j.id ORDER BY started_at DESC, id DESC LIMIT 1
        ) e ON true
        WHERE {Where} ORDER BY {Order} LIMIT @limit OFFSET @offset
        """;
    public static JobMonitoringSql Build(Guid tenantId, JobMonitoringFilter filter, int page, int pageSize)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant obrigatório.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.StartDate is { } start && filter.EndDate is { } end && start >= end)
            throw new ArgumentException("Período de criação inválido.", nameof(filter));
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page));
        if (filter.Status is { } status && !Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(filter));
        if (filter.SortDirection is not ("asc" or "desc"))
            throw new ArgumentException("Invalid sort direction.");
        var titles = filter.TypeTitles?.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        var typeOrder = titles is { Length: > 0 }
            ? "CASE j.type " + string.Join(
                " ",
                titles.Select((_, index) => $"WHEN @sort_type_{index} THEN @sort_title_{index}")) + " ELSE j.type END"
            : "j.type";
        var sortExpression = filter.SortBy switch
        {
            null or "created" => "j.created_at",
            "key" => "j.key",
            "type" => typeOrder,
            "status" => "CASE j.status WHEN 0 THEN 'Aguardando' WHEN 1 THEN 'Em processamento' WHEN 2 THEN 'Sucesso' ELSE 'Erro' END",
            "started" => "e.started_at",
            "duration" => "CASE WHEN e.started_at IS NULL THEN NULL ELSE GREATEST(0, EXTRACT(EPOCH FROM (COALESCE(e.finished_at, CURRENT_TIMESTAMP) - e.started_at))) END",
            _ => throw new ArgumentException("Invalid sort key.")
        };
        var order = filter.SortBy is null ? "j.created_at DESC, j.id DESC"
            : $"{sortExpression} {(filter.SortDirection == "asc" ? "ASC" : "DESC")} NULLS LAST, j.id ASC";
        var key = filter.KeyContains?.Trim();
        var type = filter.Type?.Trim();
        string? pattern = string.IsNullOrEmpty(key) ? null :
            "%" + key.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var where = "j.tenant_id = @tenant";
        if (pattern is not null)
            where += " AND j.key ILIKE @key ESCAPE E'\\\\'";
        if (!string.IsNullOrEmpty(type))
            where += " AND j.type = @type";
        if (filter.StartDate is not null)
            where += " AND j.created_at >= @startDate";
        if (filter.EndDate is not null)
            where += " AND j.created_at < @endDate";
        var summaryWhere = where;
        if (filter.Status is not null)
            where += " AND j.status = @status";
        return new(
            where,
            summaryWhere,
            pattern,
            string.IsNullOrEmpty(type) ? null : type,
            filter.Status,
            (page - 1) * pageSize,
            order,
            titles,
            filter.StartDate?.ToUniversalTime(),
            filter.EndDate?.ToUniversalTime());
    }
}
