using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Services.Authentication.Contracts;



namespace YallaFinance.Infrastructure.Mockup.Services;

public sealed class CatalogAuthenticationService(IUserIdentityRepository identities,
    IUserPasswordHasher passwords, IUserSessionService sessions, IPasswordChangeTicketStore passwordChangeTickets) : IAuthenticationService
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
            if (selectedTenant is null)
                return new(false, null, null, "TENANT_NOT_ALLOWED");
            // A user with a forced password change gets a ticket only, never a session (feature 28, phase 3).
            if (user.MustChangePassword)
            {
                var ticket = await passwordChangeTickets.IssueAsync(request.Email, selectedTenant.Id, token);
                return new(false, ticket, user.Name, "PASSWORD_CHANGE_REQUIRED", null, selectedTenant.Id, tenants);
            }
            return await sessions.CreateAsync(user.Id, selectedTenant, tenants, token);
        }, ct);
    }

    private static AuthenticationResult CreateInvalidAuthenticationResult() =>
        new(false, null, null, "INVALID_CREDENTIALS");
}
