using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Shared;

public sealed class JobQueries(IJobRepository repository)
{
    public Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        JobQueryValidation.Tenant(tenantId);
        return repository.GetByIdAsync(tenantId, jobId, cancellationToken);
    }

    public Task<JobPage<Job>> ListAsync(
        Guid tenantId,
        JobListFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        JobQueryValidation.Offset(tenantId, page, pageSize);
        return repository.ListAsync(tenantId, filter, page, pageSize, cancellationToken);
    }

    public Task<JobPage<JobExecution>> ListExecutionsAsync(
        Guid tenantId,
        Guid jobId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default,
        string? sortBy = null,
        string sortDirection = "desc")
    {
        JobQueryValidation.Offset(tenantId, page, pageSize);
        return repository.ListExecutionsAsync(
            tenantId,
            jobId,
            page,
            pageSize,
            cancellationToken,
            sortBy,
            sortDirection);
    }
}
