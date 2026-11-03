using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobExecutionContext : IJobExecutionContext
{
    public Guid TenantId
    {
        get; private set;
    }
    public Guid JobId
    {
        get; private set;
    }
    public Guid ExecutionId
    {
        get; private set;
    }

    public void Initialize(Guid tenantId, Guid jobId, Guid executionId)
    {
        if (TenantId != Guid.Empty ||
            tenantId == Guid.Empty ||
            jobId == Guid.Empty ||
            executionId == Guid.Empty)
            throw new InvalidOperationException("A valid execution context can only be initialized once.");
        TenantId = tenantId;
        JobId = jobId;
        ExecutionId = executionId;
    }
}
