using YallaFinance.Infrastructure.Repositories.IntegrationOperations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Common.Results;


namespace YallaFinance.Infrastructure.Database.Transactions;

public sealed class PostgresUnitOfWork(YallaFinanceDbContext db) : IUnitOfWork
{
    private IDbContextTransaction? currentTransaction;
    private bool transactionCommitted;

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (currentTransaction is not null || db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Nested transactions are not supported.");

        await using var databaseTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // CommitAsync uses this reference; the operation's finally block clears it.
        currentTransaction = databaseTransaction;
        transactionCommitted = false;
        try
        {
            return await operation(cancellationToken);
        }
        finally
        {
            try
            {
                if (!transactionCommitted)
                    await databaseTransaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                currentTransaction = null;
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<NotificationResult<int>> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is null || transactionCommitted)
            throw new InvalidOperationException("An active uncommitted unit of work is required.");

        int count;
        try
        {
            count = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            var failure = CreateExpectedCommitFailure(exception);
            if (failure is null)
                throw;
            return failure;
        }

        await currentTransaction.CommitAsync(cancellationToken);
        transactionCommitted = true;
        return NotificationResult<int>.Success(count);
    }

    internal static NotificationResult<int>? CreateExpectedCommitFailure(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
            return null;

        if (IntegrationOperationConstraint.IsIdentityConflict(postgresException))
            return NotificationResult<int>.Failure(new NotificationError(
                ErrorCodes.IntegrationOperationConflict,
                "An integration operation with this identity already exists."));

        if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            return NotificationResult<int>.Failure(new NotificationError(
                ErrorCodes.Conflict,
                "A unique persistence constraint rejected the operation."));

        return null;
    }

    internal static int GetCommitValueOrThrow(NotificationResult<int> result)
    {
        if (result.IsSuccess)
            return result.Value!;

        var error = result.Errors.FirstOrDefault();
        throw new DBConcurrencyException(error?.Message ?? "The persistence operation could not be committed.");
    }
}
