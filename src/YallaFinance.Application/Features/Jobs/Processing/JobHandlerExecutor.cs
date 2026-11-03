using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobHandlerExecutor(IEnumerable<JobHandlerRegistration> registrations)
    : IJobHandlerExecutor
{
    private readonly IReadOnlyDictionary<string, JobHandlerRegistration> handlers = registrations.ToDictionary(
        registration => registration.Type,
        StringComparer.Ordinal);

    public Task<JobHandlerResult> ExecuteAsync(
        string type,
        string payload,
        CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(type, out var registration))
            throw new KeyNotFoundException("Job type is not registered.");

        return registration.Execute(payload, cancellationToken);
    }
}
