using System.Text.Json;
using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobService(IJobRepository repository, IJobRegistry registry, TimeProvider timeProvider)
{
    public async Task<Guid> CreateAsync<TPayload>(
        Guid tenantId,
        IJobDefinition<TPayload> definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        var jobType = definition.Type;
        var jobKey = definition.BuildKey();
        if (string.IsNullOrWhiteSpace(jobType) || jobType.Length > 100 || !registry.SupportsPayload<TPayload>(jobType))
            throw new ArgumentException(
                "Job type must be registered with the matching payload contract.",
                nameof(definition));
        if (string.IsNullOrWhiteSpace(jobKey) || jobKey.Length > 250)
            throw new ArgumentException(
                "Job key must contain between 1 and 250 characters.",
                nameof(definition));
        var jobPayload = definition.BuildPayload();
        if (jobPayload is null)
            throw new ArgumentException("Job payload is required.", nameof(definition));
        var currentUtcTime = timeProvider.GetUtcNow();
        var jobRecord = new Job(
            Guid.NewGuid(),
            tenantId,
            jobType,
            jobKey,
            JsonSerializer.Serialize(
                jobPayload,
                JsonSerializerOptions.Web),
            JobStatus.Pending,
            currentUtcTime,
            currentUtcTime);
        await repository.CreateAsync(jobRecord, cancellationToken);
        return jobRecord.Id;
    }
}
