using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobHandler<TPayload>
{
    Task<JobHandlerResult> HandleAsync(TPayload payload, CancellationToken cancellationToken);
}
