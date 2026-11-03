using YallaFinance.Application.Common.Context;
using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public sealed class JobMonitoringService(
    ICurrentTenantContext currentTenant,
    IJobRepository repository,
    JobQueries jobs,
    TimeProvider time,
    IJobRegistry? registry = null)
{
    private string GetJobTypeTitle(string type) => registry?.GetTitle(type) ?? type;

    public async Task<IReadOnlyList<JobMonitoringType>> ListTypesAsync(CancellationToken cancellationToken) =>
        (await repository.ListTypesAsync(GetRequiredTenantId(), cancellationToken)).Select(type => new JobMonitoringType(
            type,
            GetJobTypeTitle(type)))
            .ToArray();

    public async Task<JobMonitoringPage<JobMonitoringItem>> ListAsync(
        JobMonitoringFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();
        JobQueryValidation.Offset(tenantId, page, pageSize);
        if (filter.SortBy == "type")
            filter = filter with
            {
                TypeTitles = (await repository.ListTypesAsync(tenantId, cancellationToken))
                    .Distinct().ToDictionary(type => type, GetJobTypeTitle)
            };
        var jobPage = await repository.ListAsync(tenantId, filter, page, pageSize, cancellationToken);
        var currentUtcTime = time.GetUtcNow();
        return new(
            jobPage.Items.Select(j => new JobMonitoringItem(
                j.Id,
                j.Type,
                j.Key,
                j.Status.ToString(),
                j.CreatedAt,
                j.UpdatedAt,
                j.StartedAt,
                j.FinishedAt,
                j.StartedAt is { } start ? CalculateDurationInSeconds(start, j.FinishedAt, currentUtcTime) : null,
                GetJobTypeTitle(j.Type))).ToArray(),
            jobPage.Page,
            jobPage.PageSize,
            jobPage.TotalItems,
            jobPage.Summary);
    }

    public async Task<JobMonitoringDetail?> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();
        var jobSnapshot = await jobs.GetByIdAsync(tenantId, jobId, cancellationToken);
        if (jobSnapshot is null || jobSnapshot.TenantId != tenantId)
            return null;
        return new(jobSnapshot.Id, jobSnapshot.Type, jobSnapshot.Key, jobSnapshot.Status.ToString(), jobSnapshot.CreatedAt, jobSnapshot.UpdatedAt,
            JobMonitoringPayload.ForDisplay(jobSnapshot.Payload), GetJobTypeTitle(jobSnapshot.Type));
    }

    public async Task<JobMonitoringExecutionPage?> ListExecutionsAsync(
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? sortBy = null,
        string sortDirection = "asc")
    {
        var tenantId = GetRequiredTenantId();
        var executionOffset = JobQueryValidation.Offset(tenantId, page, pageSize);
        var jobSnapshot = await jobs.GetByIdAsync(tenantId, jobId, cancellationToken);
        if (jobSnapshot is null || jobSnapshot.TenantId != tenantId)
            return null;
        var collectedExecutions = new List<JobExecution>();
        var sourceExecutionPageNumber = executionOffset / 100 + 1;
        var executionsToSkip = executionOffset % 100;
        // Busca resultados além do limite para identificar se há uma próxima página.
        while (collectedExecutions.Count <= pageSize)
        {
            var executionBatch = await jobs.ListExecutionsAsync(
                tenantId,
                jobId,
                sourceExecutionPageNumber++,
                100,
                cancellationToken,
                sortBy,
                sortDirection);
            collectedExecutions.AddRange(executionBatch.Items.Skip(executionsToSkip));
            executionsToSkip = 0;
            if (executionBatch.Items.Count < 100)
                break;
        }
        var currentUtcTime = time.GetUtcNow();
        var jobExecutionItems = collectedExecutions.Take(pageSize).Where(jobExecution => jobExecution.TenantId == tenantId &&
            jobExecution.JobId == jobId)
            .Select(jobExecution => new JobMonitoringExecution(
                jobExecution.Id,
                jobExecution.StartedAt,
                jobExecution.FinishedAt,
                CalculateDurationInSeconds(jobExecution.StartedAt, jobExecution.FinishedAt, currentUtcTime),
                jobExecution.FinishedAt is null ? "Processing" : jobExecution.Error is null ? "Success" : "Failed",
                jobExecution.Error is not null
                    ? JobMonitoringResultProjector.Error(jobExecution.Error) : null,
                JobMonitoringPayload.ForDisplay(jobExecution.Result),
                JobMonitoringPayload.ForDisplay(jobExecution.Details))).ToArray();
        var totalExecutionCount = await repository.CountExecutionsAsync(tenantId, jobId, cancellationToken);
        return new(jobExecutionItems, page, pageSize, collectedExecutions.Count > pageSize, totalExecutionCount);
    }

    private Guid GetRequiredTenantId() => Guid.TryParse(currentTenant.TenantId, out var tenantId) &&
        tenantId != Guid.Empty
            ? tenantId : throw new UnauthorizedAccessException("Sessão sem tenant ativo.");

    private static double CalculateDurationInSeconds(
        DateTimeOffset start,
        DateTimeOffset? finish,
        DateTimeOffset now) =>
        Math.Max(0, ((finish ?? now) - start).TotalSeconds);
}
