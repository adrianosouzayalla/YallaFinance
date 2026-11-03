using YallaFinance.Application;
using YallaFinance.Application.Common.Results;
namespace YallaFinance.Application.Common.Transactions;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
    Task<NotificationResult<int>> CommitAsync(CancellationToken cancellationToken = default);
}
