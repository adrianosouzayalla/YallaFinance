using YallaFinance.Application.Features.Auth.ChangePassword;
using YallaFinance.Application.Features.Users.CreateUser;
using YallaFinance.Application.Features.Users.ListUsers;
using YallaFinance.Application.Features.Users.SetUserActive;
using YallaFinance.Application.Features.Users.UpdateUser;
using YallaFinance.Application.Services.Users.Contracts;

namespace YallaFinance.Application.Repositories.Users;

public interface IUserRepository
{
    Task<UserPage> ListAsync(UserFilter filter, CancellationToken cancellationToken);
    Task<UserDetail> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDetail> CreateAsync(CreateUserInput input, CancellationToken cancellationToken);
    Task<UserDetail> UpdateAsync(Guid id, UpdateUserInput input, CancellationToken cancellationToken);
    Task SetUserActiveAsync(Guid id, SetUserActiveInput input, CancellationToken cancellationToken);
    Task ResetPasswordAsync(Guid id, bool confirmGlobalImpact, CancellationToken cancellationToken);
    Task ChangeOwnPasswordAsync(PasswordInput input, CancellationToken cancellationToken);
    Task<string?> GetInitialPasswordAsync(CancellationToken cancellationToken);
    Task AcquireAccountMaintenanceLockAsync(CancellationToken cancellationToken);
}
