using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Auth.Login;

public sealed record LoginResult(
    bool Success,
    string? Token,
    string? DisplayName,
    string? ErrorCode,
    DateTimeOffset? ExpiresAt,
    string? TenantId,
    IReadOnlyList<TenantOption>? Tenants)
{
    public static LoginResult From(AuthenticationResult result) => new(
        result.Success,
        result.Token,
        result.DisplayName,
        result.ErrorCode,
        result.ExpiresAt,
        result.TenantId,
        result.Tenants);
}
