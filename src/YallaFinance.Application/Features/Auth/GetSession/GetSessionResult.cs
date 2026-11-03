using YallaFinance.Application;
namespace YallaFinance.Application.Features.Auth.GetSession;

public sealed record GetSessionResult(bool Authenticated, string DisplayName, string? ExpiresAt,
    string? TenantId = null, string? ContextId = null, string? Organization = null, string? UserId = null,
    IReadOnlyList<string>? FunctionalityCodes = null);
