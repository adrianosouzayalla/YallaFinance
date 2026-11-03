namespace YallaFinance.Domain.Integrations;

public sealed record IntegrationOperation(
    Guid Id,
    Guid TenantId,
    string Type,
    string ExternalId,
    string RequestFingerprint,
    Guid JobId,
    string? ResourceType,
    Guid? ResourceId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
