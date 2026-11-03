
using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application;

namespace YallaFinance.Application.Services.Authentication.Contracts;

public sealed record AuthenticationResult(
    bool Success,
    string? Token,
    string? DisplayName,
    string? ErrorCode,
    DateTimeOffset? ExpiresAt = null,
    string? TenantId = null,
    IReadOnlyList<TenantOption>? Tenants = null);
