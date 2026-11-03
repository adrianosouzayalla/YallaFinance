using YallaFinance.Infrastructure.Repositories.Access;
using Microsoft.Extensions.Options;


using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Repositories.Authentication;
using YallaFinance.Application.Services.Authentication;

namespace YallaFinance.Infrastructure.Mockup.Services;

public sealed class IntegrationAuthenticationService(
    IOptions<JwtAuthenticationOptions> options,
    ITenantAccessRepository access)
    : IIntegrationAuthenticationService
{
    public async Task<AuthenticationResult> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Password verification remains the existing development policy in this phase.
        var valid = !string.IsNullOrWhiteSpace(request.Email) && !string.IsNullOrWhiteSpace(request.Password);
        if (!valid)
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
