using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Errors.Access;
using YallaFinance.Application.Features.AccessProfiles.Shared;
using YallaFinance.Application.Services.AccessProfiles;
using YallaFinance.Application.Services.AccessProfiles.Contracts;
using YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;
using YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;
namespace YallaFinance.Infrastructure.Repositories.Access;

public sealed class PostgresAccessProfileRepository(YallaFinanceDbContext db, CurrentSessionContext current,
    AccountMutationLock mutations, TimeProvider clock) : YallaFinance.Application.Repositories.AccessProfiles.IAccessProfileRepository
{
    private SessionContext Context => current.Value ?? throw new UserManagementException("SESSION_EXPIRED", "Sua sessão expirou.", 401);
    public async Task<IReadOnlyList<FunctionalityOption>> ListFunctionalitiesAsync(CancellationToken ct) =>
        await db.Functionalities.AsNoTracking().Where(f => f.Active)
            .OrderBy(f => f.Name).ThenBy(f => f.Code)
            .Select(f => new FunctionalityOption(f.Id, f.Code, f.Name, f.Description)).ToArrayAsync(ct);
    internal static IQueryable<AccessProfile> Query(
        YallaFinanceDbContext db,
        Guid tenantId,
        ProfileFilter filter)
    {
        var accessProfilesQuery = db.AccessProfiles.AsNoTracking().Where(profile => profile.TenantId == tenantId);
        if (filter.Active is { } isActive)
            accessProfilesQuery = accessProfilesQuery.Where(profile => profile.Active == isActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var normalizedSearch = filter.Search.Trim().ToLowerInvariant();
            accessProfilesQuery = accessProfilesQuery.Where(profile =>
                profile.Code.ToLower().Contains(normalizedSearch) || profile.Name.ToLower().Contains(normalizedSearch));
        }
        return accessProfilesQuery;
    }
    public async Task<ProfilePage> ListAsync(ProfileFilter f, CancellationToken ct)
    {
        UserManagementValidation.Paging(f.Page, f.PageSize, f.SortBy, f.SortDirection, "name", "code", "active");
        if (f.Search?.Length > 200)
            UserManagementValidation.Invalid("Filtro muito longo.");
        var accessProfilesQuery = Query(db, Context.TenantId, f);
        var count = await accessProfilesQuery.CountAsync(ct);
        var isDescending = f.SortDirection == "desc";
        var ordered = f.SortBy switch
        {
            "code" => isDescending ? accessProfilesQuery.OrderByDescending(profile => profile.Code) : accessProfilesQuery.OrderBy(profile => profile.Code),
            "active" => isDescending ? accessProfilesQuery.OrderByDescending(profile => profile.Active) : accessProfilesQuery.OrderBy(profile => profile.Active),
            _ => isDescending ? accessProfilesQuery.OrderByDescending(profile => profile.Name) : accessProfilesQuery.OrderBy(profile => profile.Name)
        };
        var items = await ordered.ThenBy(profile => profile.Id).Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .Select(profile => new ProfileDetail(profile.Id, profile.Code, profile.Name, profile.Active)).ToArrayAsync(ct);
        return new(items, count, f.Page, f.PageSize);
    }
    public async Task<ProfileDetail> GetAsync(Guid id, CancellationToken ct)
    {
        var tenantId = Context.TenantId;
        var profile = await db.AccessProfiles.AsNoTracking().Where(accessProfile => accessProfile.TenantId == tenantId && accessProfile.Id == id)
            .Select(accessProfile => new ProfileDetail(accessProfile.Id, accessProfile.Code, accessProfile.Name, accessProfile.Active))
            .SingleOrDefaultAsync(ct) ?? throw CreateProfileNotFoundException();
        var functionalityIds = await db.AccessProfileFunctionalities.AsNoTracking()
            .Where(link => link.TenantId == tenantId && link.ProfileId == id && link.Functionality.Active)
            .Select(link => link.FunctionalityId).ToArrayAsync(ct);
        return profile with
        {
            FunctionalityIds = functionalityIds
        };
    }
    public async Task<ProfileDetail> CreateAsync(CreateProfileInput input, CancellationToken ct)
    {
        var code = UserManagementValidation.ProfileCode(input.Code);
        var name = UserManagementValidation.Name(input.Name, 100);
        var profileId = await ExecuteProfileMutationAsync(async token =>
        {
            if (await db.AccessProfiles.AnyAsync(accessProfile =>
                    accessProfile.TenantId == Context.TenantId && accessProfile.Code == code, token))
                throw new UserManagementException("PROFILE_CONFLICT", "Código de perfil já utilizado.", 409);
            var now = clock.GetUtcNow().UtcDateTime;
            var newProfile = new AccessProfile
            {
                Id = Guid.NewGuid(),
                TenantId = Context.TenantId,
                Code = code,
                Name = name,
                Active = input.Active,
            };
            db.AccessProfiles.Add(newProfile);
            await ReconcileAccessProfileFunctionalitiesAsync(newProfile, input.FunctionalityIds, token);
            return newProfile.Id;
        }, ct);
        return await GetAsync(profileId, ct);
    }
    public async Task<ProfileDetail> UpdateAsync(Guid id, UpdateProfileInput input, CancellationToken ct)
    {
        if (input.FunctionalityIds is null)
            throw new UserManagementException("INVALID_FUNCTIONALITY", "Informe a seleção completa de funcionalidades.");
        var name = UserManagementValidation.Name(input.Name, 100);
        await ExecuteProfileMutationAsync(async token =>
        {
            var accessProfile = await FindAccessProfileAsync(id, token);
            accessProfile.Name = name;
            await ReconcileAccessProfileFunctionalitiesAsync(accessProfile, input.FunctionalityIds, token);
            return true;
        }, ct);
        return await GetAsync(id, ct);
    }
    private async Task ReconcileAccessProfileFunctionalitiesAsync(
        AccessProfile accessProfile,
        IReadOnlyCollection<Guid>? requestedFunctionalityIds,
        CancellationToken ct)
    {
        var tenantId = Context.TenantId;
        var activeFunctionalityIds = await db.Functionalities
            .Where(functionality => functionality.Active)
            .Select(functionality => functionality.Id)
            .ToArrayAsync(ct);
        var existingProfileFunctionalities = await db.AccessProfileFunctionalities
            .Where(profileFunctionality => profileFunctionality.TenantId == tenantId && profileFunctionality.ProfileId == accessProfile.Id)
            .ToArrayAsync(ct);
        var inactiveFunctionalityIds = await db.AccessProfileFunctionalities.AsNoTracking()
            .Where(profileFunctionality => profileFunctionality.TenantId == tenantId &&
                profileFunctionality.ProfileId == accessProfile.Id && !profileFunctionality.Functionality.Active)
            .Select(profileFunctionality => profileFunctionality.FunctionalityId)
            .ToArrayAsync(ct);
        var selectedFunctionalityIds = AccessProfileFunctionalityRules.ResolveSelection(
            requestedFunctionalityIds,
            activeFunctionalityIds,
            inactiveFunctionalityIds).ToHashSet();
        foreach (var profileFunctionality in existingProfileFunctionalities.Where(profileFunctionality =>
                     activeFunctionalityIds.Contains(profileFunctionality.FunctionalityId) &&
                     !selectedFunctionalityIds.Contains(profileFunctionality.FunctionalityId)))
            db.AccessProfileFunctionalities.Remove(profileFunctionality);

        var existingFunctionalityIds = existingProfileFunctionalities
            .Select(profileFunctionality => profileFunctionality.FunctionalityId)
            .ToHashSet();
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var functionalityId in selectedFunctionalityIds.Where(functionalityId =>
                     !existingFunctionalityIds.Contains(functionalityId)))
            db.AccessProfileFunctionalities.Add(new AccessProfileFunctionality
            {
                TenantId = tenantId,
                ProfileId = accessProfile.Id,
                FunctionalityId = functionalityId,
            });
    }
    public Task SetActiveAsync(Guid id, bool active, CancellationToken ct) => ExecuteProfileMutationAsync(async token =>
    {
        var accessProfile = await FindAccessProfileAsync(id, token);
        if (accessProfile.Code == "ADMIN" && !active)
            throw new UserManagementException("PROFILE_CONFLICT", "O perfil ADMIN deve permanecer ativo.", 409);
        accessProfile.Active = active;
        return true;
    }, ct);
    private async Task<AccessProfile> FindAccessProfileAsync(Guid accessProfileId, CancellationToken ct) =>
        await db.AccessProfiles.SingleOrDefaultAsync(profile =>
            profile.Id == accessProfileId && profile.TenantId == Context.TenantId, ct) ?? throw CreateProfileNotFoundException();
    private Task<T> ExecuteProfileMutationAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        mutations.ExecuteAsync(async token =>
        {
            var now = clock.GetUtcNow().UtcDateTime;
            if (!await db.UserSessions.AnyAsync(s => s.Id == Context.SessionId && s.UserId == Context.UserId && s.TenantId == Context.TenantId
                && s.RevokedAt == null && s.ExpiresAt > now && s.User.Active && s.TenantAccess.Active && s.Tenant.Active, token))
                throw new UserManagementException("SESSION_EXPIRED", "Sua sessão expirou.", 401);
            return await action(token);
        }, ct);
    private static UserManagementException CreateProfileNotFoundException() =>
        new("PROFILE_NOT_FOUND", "Perfil não encontrado.", 404);
}
