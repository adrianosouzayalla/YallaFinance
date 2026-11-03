using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Jobs;

public interface IJobRepository
{
    Task CreateAsync(Job job, CancellationToken cancellationToken);
    Task<ClaimedJob?> TryClaimNextAsync(CancellationToken cancellationToken);
    Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken);
    Task<JobPage<Job>> ListAsync(Guid tenantId, JobListFilter filter, int page, int pageSize, CancellationToken cancellationToken);
    Task<JobPage<JobExecution>> ListExecutionsAsync(
        Guid tenantId,
        Guid jobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? sortBy = null,
        string sortDirection = "desc");
    Task<long> CountExecutionsAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListTypesAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<JobMonitoringPage<JobMonitoringRow>> ListAsync(
        Guid tenantId,
        JobMonitoringFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> TryRequeueAsync(
        Guid tenantId,
        Guid jobId,
        DateTimeOffset? expectedUpdatedAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
