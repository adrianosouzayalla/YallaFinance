using Microsoft.EntityFrameworkCore;



using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Common.Errors.Access;
using YallaFinance.Application.Services.Users.Contracts;
using YallaFinance.Application.Features.Users.ListUsers;
using YallaFinance.Application.Features.Users.CreateUser;
using YallaFinance.Application.Features.Users.UpdateUser;
using YallaFinance.Application.Features.Users.SetUserActive;
using YallaFinance.Application.Features.Auth.ChangePassword;
using YallaFinance.Application.Repositories.Settings;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Infrastructure.Database.Transactions;
namespace YallaFinance.Infrastructure.Repositories.Access;

public sealed class PostgresUserRepository(YallaFinanceDbContext db, CurrentSessionContext current,
    IUnitOfWork unitOfWork, IUserPasswordHasher passwords, ITenantSettingRepository settings, TimeProvider clock,
    CurrentExecutionActorContext actor)
    : YallaFinance.Application.Repositories.Users.IUserRepository
{
    public const string SettingsGroup = "UserRegistration";
    public const string InitialPasswordKey = "InitialPassword";
    private SessionContext Context => current.Value ?? throw new UserManagementException("SESSION_EXPIRED", "Sua sessão expirou.", 401);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task AcquireAccountMaintenanceLockAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({AccountMutationLock.Scope}, 0))",
            cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<TenantAccess, UserDetail>> Projection =
        tenantAccess => new UserDetail(
            tenantAccess.UserId,
            tenantAccess.User.Login,
            tenantAccess.User.Name,
            tenantAccess.UserEmail,
            tenantAccess.User.Active,
            tenantAccess.Active,
            tenantAccess.ProfileId,
            tenantAccess.AccessProfile == null ? null : tenantAccess.AccessProfile.Name);
    internal static IQueryable<UserDetail> UserQuery(YallaFinanceDbContext db, Guid tenant, UserFilter f)
        => BuildTenantAccessQuery(db, tenant, f).Select(Projection);
    private static IQueryable<TenantAccess> BuildTenantAccessQuery(YallaFinanceDbContext db, Guid tenant, UserFilter f)
    {
        var tenantAccessQuery = db.TenantAccesses.AsNoTracking().Where(tenantAccess => tenantAccess.TenantId == tenant);
        if (f.UserActive is { } isUserActive)
            tenantAccessQuery = tenantAccessQuery.Where(tenantAccess => tenantAccess.User.Active == isUserActive);
        if (f.AccessActive is { } isTenantAccessActive)
            tenantAccessQuery = tenantAccessQuery.Where(tenantAccess => tenantAccess.Active == isTenantAccessActive);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var normalizedSearch = f.Search.Trim().ToLowerInvariant();
            tenantAccessQuery = tenantAccessQuery.Where(tenantAccess =>
                tenantAccess.User.Login.Contains(normalizedSearch) ||
                tenantAccess.User.Name.ToLower().Contains(normalizedSearch) ||
                (tenantAccess.UserEmail != null && tenantAccess.UserEmail.Contains(normalizedSearch)));
        }
        return tenantAccessQuery;
    }
    public async Task<UserPage> ListAsync(UserFilter f, CancellationToken ct)
    {
        UserManagementValidation.Paging(f.Page, f.PageSize, f.SortBy, f.SortDirection, "name", "login", "email", "active", "accessActive");
        if (f.Search?.Length > 200)
            UserManagementValidation.Invalid("Filtro muito longo.");
        // Apply sorting to mapped entity expressions before DTO projection (EF constructor projections cannot be recomposed).
        var tenantAccessQuery = BuildTenantAccessQuery(db, Context.TenantId, f);
        var count = await tenantAccessQuery.CountAsync(ct);
        var isDescending = f.SortDirection == "desc";
        var ordered = f.SortBy switch
        {
            "login" => isDescending ? tenantAccessQuery.OrderByDescending(tenantAccess => tenantAccess.User.Login) : tenantAccessQuery.OrderBy(tenantAccess => tenantAccess.User.Login),
            "email" => isDescending ? tenantAccessQuery.OrderByDescending(tenantAccess => tenantAccess.UserEmail) : tenantAccessQuery.OrderBy(tenantAccess => tenantAccess.UserEmail),
            "active" => isDescending ? tenantAccessQuery.OrderByDescending(tenantAccess => tenantAccess.User.Active) : tenantAccessQuery.OrderBy(tenantAccess => tenantAccess.User.Active),
            "accessActive" => isDescending ? tenantAccessQuery.OrderByDescending(tenantAccess => tenantAccess.Active) : tenantAccessQuery.OrderBy(tenantAccess => tenantAccess.Active),
            _ => isDescending ? tenantAccessQuery.OrderByDescending(tenantAccess => tenantAccess.User.Name) : tenantAccessQuery.OrderBy(tenantAccess => tenantAccess.User.Name)
        };
        var rows = await ordered.ThenBy(tenantAccess => tenantAccess.UserId).Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
            .Select(Projection).ToArrayAsync(ct);
        return new(rows, count, f.Page, f.PageSize);
    }
    public async Task<UserDetail> GetAsync(Guid id, CancellationToken ct)
    {
        // Filter entities first; never compose predicates over a record constructor.
        var userDetail = await db.TenantAccesses.AsNoTracking()
            .Where(tenantAccess => tenantAccess.TenantId == Context.TenantId && tenantAccess.UserId == id)
            .Select(Projection).SingleOrDefaultAsync(ct);
        return userDetail ?? throw CreateUserNotFoundException();
    }
    public async Task<string?> GetInitialPasswordAsync(CancellationToken ct)
    {
        var values = await settings.ReadGroupAsync(Context.TenantId, SettingsGroup, ct);
        return values.GetValueOrDefault(InitialPasswordKey);
    }
    public async Task<UserDetail> CreateAsync(CreateUserInput input, CancellationToken ct)
    {
        var login = UserManagementValidation.Login(input.Login);
        var name = UserManagementValidation.Name(input.Name);
        var email = UserManagementValidation.Email(input.Email);
        var userId = await ExecuteUserMutationAsync(async token =>
        {
            var initialPassword = await GetInitialPasswordAsync(token);
            if (initialPassword is null)
                throw new UserManagementException("INITIAL_PASSWORD_REQUIRED", "Configure a senha inicial da empresa antes de cadastrar usuários.", 409);
            var passwordHash = passwords.Hash(initialPassword);
            await EnsureIdentityIsUniqueAsync(Guid.Empty, login, email, Context.TenantId, token);
            await EnsureProfileIsAssignableAsync(Context.TenantId, input.ProfileId, null, token);
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Login = login,
                Name = name,
                PasswordHash = passwordHash,
                // The company's initial password must be replaced at the first login (feature 28, phase 3).
                MustChangePassword = true,
                Active = input.Active,
            };
            db.Users.Add(newUser);
            db.TenantAccesses.Add(new TenantAccess
            {
                Id = Guid.NewGuid(),
                TenantId = Context.TenantId,
                UserId = newUser.Id,
                UserEmail = email,
                ProfileId = input.ProfileId,
                Active = true,
            });
            return newUser.Id;
        }, ct);
        return await GetAsync(userId, ct);
    }
    public async Task<UserDetail> UpdateAsync(Guid id, UpdateUserInput input, CancellationToken ct)
    {
        var name = UserManagementValidation.Name(input.Name);
        var email = UserManagementValidation.Email(input.Email);
        await ExecuteUserMutationAsync(async token =>
        {
            var tenantAccess = await FindTenantAccessAsync(id, token);
            await EnsureIdentityIsUniqueAsync(id, tenantAccess.User.Login, email, Context.TenantId, token);
            await EnsureProfileIsAssignableAsync(Context.TenantId, input.ProfileId, tenantAccess.ProfileId, token);
            tenantAccess.User.Name = name;
            tenantAccess.UserEmail = email;
            tenantAccess.ProfileId = input.ProfileId;
            return true;
        }, ct);
        return await GetAsync(id, ct);
    }
    public Task SetUserActiveAsync(Guid id, SetUserActiveInput input, CancellationToken ct) => ExecuteUserMutationAsync(async token =>
    {
        var tenantAccess = await FindTenantAccessAsync(id, token);
        if (!input.ConfirmGlobalImpact)
            UserManagementValidation.Invalid("Confirme o impacto em todas as empresas.");
        if (id == Context.UserId && !input.Active)
            throw CreateUserConflictException("Não é permitido desativar o próprio usuário.");
        if (tenantAccess.User.Active != input.Active)
        {
            tenantAccess.User.Active = input.Active;
            await RevokeActiveSessionsAsync(id, token);
        }
        return true;
    }, ct);
    public Task ResetPasswordAsync(Guid id, bool confirmGlobalImpact, CancellationToken ct) => ExecuteUserMutationAsync(async token =>
    {
        if (id == Context.UserId)
            UserManagementValidation.Invalid("Use Alterar senha no menu de opções da sessão para modificar sua credencial.");
        if (!confirmGlobalImpact)
            UserManagementValidation.Invalid("Confirme o impacto em todas as empresas.");
        var tenantAccess = await FindTenantAccessAsync(id, token);
        var initialPassword = await GetInitialPasswordAsync(token);
        if (initialPassword is null)
            throw new UserManagementException("INITIAL_PASSWORD_REQUIRED", "Configure a senha inicial da empresa antes de redefinir senhas.", 409);
        tenantAccess.User.PasswordHash = passwords.Hash(initialPassword);
        tenantAccess.User.MustChangePassword = true;
        await RevokeActiveSessionsAsync(id, token);
        return true;
    }, ct);
    public Task ChangeOwnPasswordAsync(PasswordInput input, CancellationToken ct) => ExecuteUserMutationAsync(async token =>
    {
        var tenantAccess = await FindTenantAccessAsync(Context.UserId, token);
        if (!passwords.Verify(tenantAccess.User.PasswordHash, input.CurrentPassword))
            throw new UserManagementException("INVALID_PASSWORD", "Senha atual incorreta.");
        tenantAccess.User.PasswordHash = passwords.Hash(input.NewPassword);
        tenantAccess.User.MustChangePassword = false;
        await RevokeActiveSessionsAsync(Context.UserId, token);
        return true;
    }, ct);
    private async Task<T> ExecuteUserMutationAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        => await unitOfWork.ExecuteAsync(async token =>
        {
            await AcquireAccountMaintenanceLockAsync(token);
            // Recheck the operator after obtaining the maintenance lock.
            await RequireOperatorAsync(token);
            var eligibleAdminTenantIdsBeforeChange = await EligibleAdminTenants(db, token);
            var result = await action(token);
            await db.SaveChangesAsync(token);
            var eligibleAdminTenantIdsAfterChange = await EligibleAdminTenants(db, token);
            if (eligibleAdminTenantIdsBeforeChange.Except(eligibleAdminTenantIdsAfterChange).Any())
                throw CreateUserConflictException("A alteração removeria o último administrador habilitado de uma empresa.");
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
            return result;
        }, ct);
    internal static Task<Guid[]> EligibleAdminTenants(YallaFinanceDbContext db, CancellationToken ct) =>
        db.TenantAccesses.Where(a => a.Active && a.User.Active && a.User.PasswordHash != null && a.Tenant.Active
            && a.AccessProfile != null && a.AccessProfile.Active && a.AccessProfile.Code == "ADMIN")
            .Select(a => a.TenantId).Distinct().ToArrayAsync(ct);
    private async Task RequireOperatorAsync(CancellationToken ct)
    {
        if (!await db.UserSessions.AnyAsync(s => s.Id == Context.SessionId && s.UserId == Context.UserId && s.TenantId == Context.TenantId
            && s.RevokedAt == null && s.ExpiresAt > Now && s.User.Active && s.TenantAccess.Active && s.Tenant.Active, ct))
            throw new UserManagementException("SESSION_EXPIRED", "Sua sessão expirou.", 401);
    }
    private async Task<TenantAccess> FindTenantAccessAsync(Guid userId, CancellationToken ct) =>
        await db.TenantAccesses.Include(tenantAccess => tenantAccess.User).SingleOrDefaultAsync(tenantAccess =>
            tenantAccess.UserId == userId && tenantAccess.TenantId == Context.TenantId, ct) ?? throw CreateUserNotFoundException();
    private async Task EnsureProfileIsAssignableAsync(
        Guid tenantId,
        Guid? profileId,
        Guid? existingProfileId,
        CancellationToken ct)
    {
        if (profileId is null)
            return;
        if (!await db.AccessProfiles.AnyAsync(profile =>
                profile.Id == profileId && profile.TenantId == tenantId &&
                (profile.Active || profile.Id == existingProfileId), ct))
            UserManagementValidation.Invalid("Perfil inválido para esta empresa.");
    }
    private async Task EnsureIdentityIsUniqueAsync(
        Guid userId,
        string login,
        string email,
        Guid tenantId,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(user => user.Id != userId && user.Login == login, ct) ||
            await db.TenantAccesses.AnyAsync(tenantAccess =>
                tenantAccess.TenantId == tenantId && tenantAccess.UserId != userId && tenantAccess.UserEmail == email, ct))
            throw CreateUserConflictException("Login ou e-mail já utilizado.");
    }
    private Task<int> RevokeActiveSessionsAsync(Guid userId, CancellationToken ct)
    {
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actor, clock);
        return db.UserSessions.Where(session => session.UserId == userId && session.RevokedAt == null)
            .ExecuteUpdateAsync(sessions => sessions.SetProperty(session => session.RevokedAt, Now)
                .SetProperty(session => session.UpdatedAt, audit.UtcNow).SetProperty(session => session.UpdatedBy, audit.ActorId), ct);
    }
    private static UserManagementException CreateUserNotFoundException() =>
        new("USER_NOT_FOUND", "Registro não encontrado.", 404);
    private static UserManagementException CreateUserConflictException(string message) =>
        new("USER_CONFLICT", message, 409);
}
