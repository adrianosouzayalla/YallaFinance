using YallaFinance.Application;
namespace YallaFinance.Application.Common.Context;

public sealed record SessionContext(Guid SessionId, Guid UserId, Guid TenantId,
    string DisplayName, string? Email, DateTimeOffset ExpiresAt, string? Organization = null);
