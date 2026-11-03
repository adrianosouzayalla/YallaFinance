using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobHandlerExecutor
{
    Task<JobHandlerResult> ExecuteAsync(
        string type,
        string payload,
        CancellationToken cancellationToken);
}
