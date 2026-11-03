using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobRegistry(IEnumerable<JobRegistration> registrations) : IJobRegistry
{
    private readonly IReadOnlyDictionary<string, JobRegistration> entries = registrations.ToDictionary(
        r => r.Type,
        StringComparer.Ordinal);

    public bool Contains(string type) => entries.ContainsKey(type);

    public string GetTitle(string type) => entries.TryGetValue(type, out var jobRegistration) ? jobRegistration.JobTitle : type;

    public bool SupportsPayload<TPayload>(string type) => entries.TryGetValue(type, out var jobRegistration) &&
        jobRegistration.PayloadType == typeof(TPayload);

}
