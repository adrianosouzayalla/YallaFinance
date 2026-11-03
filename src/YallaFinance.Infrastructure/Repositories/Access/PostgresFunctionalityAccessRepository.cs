using Microsoft.EntityFrameworkCore;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.Infrastructure.Repositories.Access;

public sealed class PostgresFunctionalityAccessRepository(YallaFinanceDbContext db, TimeProvider clock) : YallaFinance.Application.Repositories.Authorization.IFunctionalityAccessRepository
{
    public async Task<bool> HasAnyAsync(SessionContext session, IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        if (codes.Count == 0)
            return false;
        var normalizedFunctionalityCodes = codes.Select(code => code.ToUpperInvariant()).Distinct().ToArray();
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.UserSessions.AsNoTracking().AnyAsync(s =>
            s.Id == session.SessionId && s.UserId == session.UserId && s.TenantId == session.TenantId &&
            s.RevokedAt == null && s.ExpiresAt > now && s.User.Active && s.Tenant.Active && s.TenantAccess.Active &&
            s.TenantAccess.ProfileId != null && s.TenantAccess.AccessProfile != null && s.TenantAccess.AccessProfile.Active &&
            db.AccessProfileFunctionalities.Any(link => link.TenantId == session.TenantId &&
                link.ProfileId == s.TenantAccess.ProfileId && link.Functionality.Active &&
                normalizedFunctionalityCodes.Contains(link.Functionality.Code.ToUpper())), ct);
    }

    public async Task<IReadOnlyList<string>> ListGrantedCodesAsync(SessionContext session, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var profileId = await db.UserSessions.AsNoTracking().Where(s =>
                s.Id == session.SessionId && s.UserId == session.UserId && s.TenantId == session.TenantId &&
                s.RevokedAt == null && s.ExpiresAt > now && s.User.Active && s.Tenant.Active && s.TenantAccess.Active &&
                s.TenantAccess.ProfileId != null && s.TenantAccess.AccessProfile != null && s.TenantAccess.AccessProfile.Active)
            .Select(s => s.TenantAccess.ProfileId).SingleOrDefaultAsync(ct);
        if (profileId is null)
            return [];
        return await db.AccessProfileFunctionalities.AsNoTracking()
            .Where(link => link.TenantId == session.TenantId && link.ProfileId == profileId && link.Functionality.Active)
            .Select(link => link.Functionality.Code).Distinct().OrderBy(code => code).ToArrayAsync(ct);
    }
}
