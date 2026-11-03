using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobExecutionContext
{
    Guid TenantId
    {
        get;
    }
    Guid JobId
    {
        get;
    }
    Guid ExecutionId
    {
        get;
    }
}
