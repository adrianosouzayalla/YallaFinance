using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Repositories.Users.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Users;

public interface IUserIdentityRepository
{
    Task<Guid?> FindOperationalUserIdAsync(string login, CancellationToken ct);
    Task<T> ExecuteLockedAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
    Task<UserCredentialIdentity?> ResolveAsync(string identifier, CancellationToken ct);
    Task<IReadOnlyList<TenantOption>> TenantsAsync(Guid userId, CancellationToken ct);
    // Validates the password rule, stores the new hash, clears the forced-change flag and revokes the user's active sessions.
    Task ReplacePasswordAsync(Guid userId, string newPassword, CancellationToken ct);
}
