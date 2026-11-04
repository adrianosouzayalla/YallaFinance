using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Authentication.Errors;
using YallaFinance.Application.Repositories.Users.Contracts;
using YallaFinance.Application.Entities.Tenants;
namespace YallaFinance.Infrastructure.Repositories.Access;

public sealed class PostgresUserIdentityRepository(YallaFinanceDbContext db, AccountMutationLock mutations,
    YallaFinance.Application.Services.Authentication.IUserPasswordHasher passwords) : IUserIdentityRepository
{
    public Task<Guid?> FindOperationalUserIdAsync(string login, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(user => user.Login == login && user.IsOperational
            && !user.Active && user.PasswordHash == null
            && !db.TenantAccesses.Any(access => access.UserId == user.Id)
            && !db.UserSessions.Any(session => session.UserId == user.Id))
            .Select(user => (Guid?)user.Id).SingleOrDefaultAsync(ct);

    public async Task<T> ExecuteLockedAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        try
        {
            return await mutations.ExecuteAsync(action, ct);
        }
        catch (Exception error) when (error is System.Data.Common.DbException or TimeoutException)
        {
            throw new SessionUnavailableException();
        }
    }
    public async Task<UserCredentialIdentity?> ResolveAsync(string identifier, CancellationToken ct)
    {
        var normalizedLogin = identifier.Trim().ToLowerInvariant();
        return await db.Users.AsNoTracking().Where(u => u.Login == normalizedLogin && u.Active && !u.IsOperational).Select(u => new UserCredentialIdentity
        {
            Id = u.Id,
            Name = u.Name,
            PasswordHash = u.PasswordHash,
            MustChangePassword = u.MustChangePassword,
        }).SingleOrDefaultAsync(ct);
    }

    public async Task ReplacePasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        UserManagementValidation.Password(newPassword);
        var now = DateTime.UtcNow;
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        user.PasswordHash = passwords.Hash(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = now;
        user.UpdatedBy = userId;
        await db.UserSessions.Where(session => session.UserId == userId && session.RevokedAt == null)
            .ExecuteUpdateAsync(sessions => sessions.SetProperty(session => session.RevokedAt, now)
                .SetProperty(session => session.UpdatedAt, now).SetProperty(session => session.UpdatedBy, userId), ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TenantOption>> TenantsAsync(Guid userId, CancellationToken ct)
        => await db.TenantAccesses.AsNoTracking().Where(a => a.UserId == userId && a.Active && a.User.Active && !a.User.IsOperational && a.Tenant.Active)
            .OrderBy(a => a.Tenant.Name).ThenBy(a => a.TenantId)
            .Select(a => new TenantOption(a.TenantId.ToString(), a.Tenant.Name)).ToArrayAsync(ct);
}
