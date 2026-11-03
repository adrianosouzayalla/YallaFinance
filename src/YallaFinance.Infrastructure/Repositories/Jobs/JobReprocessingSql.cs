namespace YallaFinance.Infrastructure.Repositories.Jobs;

public static class JobReprocessingSql
{
    public const string Requeue = """
        UPDATE core.job SET status = 0, updated_at = @auditNow, updated_by = @auditActor
        WHERE tenant_id = @tenant AND id = @job AND status = 3 AND updated_at IS NOT DISTINCT FROM @expectedUpdatedAt
        """;
}
