using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Settings;

namespace YallaFinance.Application.Features.Jobs.Retry;

public sealed class JobRetrySweeper(
    IJobRetryRepository retries,
    IJobRepository jobs,
    ITenantSettingRepository settings,
    TimeProvider clock)
{
    public const string RetryLimitGroup = "job_retry_limit";
    private const int BatchSize = 100;

    // Reenfileira Jobs em Failed cujo tipo esta configurado, dentro do limite e com a tentativa vencida.
    public async Task<int> RequeueDueJobsAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var limitsByTenant = new Dictionary<Guid, IReadOnlyDictionary<string, string>>();
        var requeued = 0;
        foreach (var candidate in await retries.ListFailedJobsForRetryAsync(BatchSize, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tenantId = candidate.Job.TenantId;
            if (!limitsByTenant.TryGetValue(tenantId, out var limits))
            {
                limits = await settings.ReadGroupAsync(tenantId, RetryLimitGroup, cancellationToken);
                limitsByTenant[tenantId] = limits;
            }

            var limit = JobRetryPolicy.ParseLimit(limits.GetValueOrDefault(candidate.Job.Type));
            if (!JobRetryPolicy.IsDue(candidate.Attempts, limit, candidate.LastFinishedAt, candidate.Job.Id, now))
                continue;
            if (await jobs.TryRequeueAsync(tenantId, candidate.Job.Id, candidate.Job.UpdatedAt, now, cancellationToken))
                requeued++;
        }

        return requeued;
    }
}
