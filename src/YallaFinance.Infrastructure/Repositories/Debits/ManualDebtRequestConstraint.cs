using Npgsql;
namespace YallaFinance.Infrastructure.Repositories.Debits;

public static class ManualDebtRequestConstraint
{
    public static bool IsIdentityConflict(PostgresException error) => error.SqlState == PostgresErrorCodes.UniqueViolation
        && error.ConstraintName == "pk_manual_debt_request";
}
