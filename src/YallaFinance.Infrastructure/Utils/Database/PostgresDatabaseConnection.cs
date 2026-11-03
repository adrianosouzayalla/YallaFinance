using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using YallaFinance.Infrastructure.Database.Generated;

namespace YallaFinance.Infrastructure.Utils.Database;

// Borrows the unit-of-work connection when one exists; otherwise owns a short-lived connection.
internal sealed class PostgresDatabaseConnection(YallaFinanceDbContext db)
{
    public async Task<T> ExecuteAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction?, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is { } transaction)
            return await operation(
                (NpgsqlConnection)db.Database.GetDbConnection(),
                (NpgsqlTransaction)transaction.GetDbTransaction());

        await using var connection = await OpenAsync(cancellationToken);
        return await operation(connection, null);
    }

    public void RequireTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("An active unit of work is required.");
    }

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Job database connection is not configured."));
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
