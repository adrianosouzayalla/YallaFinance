using YallaFinance.Infrastructure.Repositories.Access;
using Microsoft.Extensions.Options;


using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Repositories.Authentication;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Authentication;

namespace YallaFinance.Infrastructure.Mockup.Services;

public sealed class IntegrationAuthenticationService(
    IOptions<JwtAuthenticationOptions> options,
    ITenantAccessRepository access,
    IUserIdentityRepository identities,
    IUserPasswordHasher passwords)
    : IIntegrationAuthenticationService
{
    public async Task<AuthenticationResult> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var valid = !string.IsNullOrWhiteSpace(request.Email) && !string.IsNullOrWhiteSpace(request.Password) && request.Password.Length <= 128;
        if (!valid)
            return new AuthenticationResult(false, null, null, "INVALID_CREDENTIALS");

        // The stored hash must match before any company is returned, as in the interactive login.
        var credentialsVerified = await identities.ExecuteLockedAsync(async token =>
        {
            var user = await identities.ResolveAsync(request.Email, token);
            return user is not null && passwords.Verify(user.PasswordHash, request.Password);
        }, cancellationToken);
        if (!credentialsVerified)
            return new AuthenticationResult(false, null, null, "INVALID_CREDENTIALS");

        var email = TenantAccessEmail.Normalize(request.Email);
        var tenants = await access.GetAuthorizedTenantsAsync(email, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.TenantId) && tenants.Count > 1)
            return new AuthenticationResult(
                false,
                null,
                "Usuário Yalla",
                "TENANT_SELECTION_REQUIRED",
                null,
                null,
                tenants);
        var active = string.IsNullOrWhiteSpace(request.TenantId)
            ? tenants.FirstOrDefault()
            : tenants.FirstOrDefault(item => string.Equals(
                item.Id,
                request.TenantId,
                StringComparison.OrdinalIgnoreCase));
        return active is null
            ? new AuthenticationResult(false, null, null, "TENANT_NOT_ALLOWED")
            : CatalogJwtTokenIssuer.Create(options.Value, email, active, tenants);
    }
}
