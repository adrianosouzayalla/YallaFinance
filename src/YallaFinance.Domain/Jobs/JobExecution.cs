namespace YallaFinance.Domain.Jobs;

public sealed record JobExecution(
    Guid Id,
    Guid TenantId,
    Guid JobId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt = null,
    string? Result = null,
    string? Error = null,
    string? Details = null);
