using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Infrastructure.Database.Transactions;
namespace YallaFinance.Infrastructure.Repositories.Access;
// Shared by login, credential provisioning and every account/profile mutation.
// A single lock deliberately favors correctness over throughput for this small directory.
public sealed class AccountMutationLock(IUserRepository users, IUnitOfWork unit)
{
    public const string Scope = "yalla-account-maintenance-v1";
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        => unit.ExecuteAsync(async token =>
        {
            await users.AcquireAccountMaintenanceLockAsync(token);
            var result = await action(token);
            PostgresUnitOfWork.GetCommitValueOrThrow(await unit.CommitAsync(token));
            return result;
        }, ct);
}
