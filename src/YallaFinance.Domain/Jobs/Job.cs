namespace YallaFinance.Domain.Jobs;

public sealed record Job(
    Guid Id,
    Guid TenantId,
    string Type,
    string Key,
    string Payload,
    JobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
