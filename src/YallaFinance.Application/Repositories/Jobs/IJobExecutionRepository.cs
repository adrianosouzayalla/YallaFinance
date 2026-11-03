using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs;

public interface IJobExecutionRepository
{
    Task CompleteAsync(
        Guid tenantId,
        Guid jobId,
        Guid executionId,
        JobHandlerResult result,
        CancellationToken cancellationToken);
}
