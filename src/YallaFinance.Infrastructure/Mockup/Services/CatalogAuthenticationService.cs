using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Services.Authentication.Contracts;



namespace YallaFinance.Infrastructure.Mockup.Services;

public sealed class CatalogAuthenticationService(IUserIdentityRepository identities,
    IUserPasswordHasher passwords, IUserSessionService sessions) : IAuthenticationService
{
    public Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 128)
            return Task.FromResult(CreateInvalidAuthenticationResult());
        // Shared lock covers verification, tenant selection and session creation.
        return identities.ExecuteLockedAsync(async token =>
        {
            var user = await identities.ResolveAsync(request.Email, token);
            if (user is null || !passwords.Verify(user.PasswordHash, request.Password))
                return CreateInvalidAuthenticationResult();
            var tenants = await identities.TenantsAsync(user.Id, token);
            if (string.IsNullOrWhiteSpace(request.TenantId) && tenants.Count > 1)
                return new(false, null, user.Name, "TENANT_SELECTION_REQUIRED", null, null, tenants);
            var selectedTenant = string.IsNullOrWhiteSpace(request.TenantId) ? tenants.FirstOrDefault() :
                tenants.FirstOrDefault(t => string.Equals(t.Id, request.TenantId, StringComparison.OrdinalIgnoreCase));
            return selectedTenant is null ? new(false, null, null, "TENANT_NOT_ALLOWED") :
                await sessions.CreateAsync(user.Id, selectedTenant, tenants, token);
        }, ct);
    }
    private static AuthenticationResult CreateInvalidAuthenticationResult() =>
        new(false, null, null, "INVALID_CREDENTIALS");
}
