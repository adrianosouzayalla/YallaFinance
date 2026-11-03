using Npgsql;
namespace YallaFinance.Infrastructure.Repositories.IntegrationOperations;

public static class IntegrationOperationConstraint
{
    public static bool IsIdentityConflict(PostgresException error) => error.SqlState == PostgresErrorCodes.UniqueViolation
        && error.ConstraintName == "uk_integration_operation_identity";
}
