using YallaFinance.Infrastructure.Database.Transactions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Common.Transactions;


namespace YallaFinance.Infrastructure.Repositories.Charges;

internal sealed class PostgresChargeSettlementStore(
    YallaFinanceDbContext db,
    IUnitOfWork unitOfWork,
    TimeProvider clock, YallaFinance.Application.Common.Context.CurrentExecutionActorContext actorContext)
{
    internal sealed record ChargeSettlementChargeRow(Guid TenantId, Guid ChargeId, decimal Amount);

    public async Task<ChargeSettlementSnapshot?> LoadAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || chargeId == Guid.Empty)
            return null;
        var charge = await ChargeQuery(db, tenantId, chargeId).SingleOrDefaultAsync(cancellationToken);
        if (charge is null)
            return null;
        var items = await ItemQuery(db, tenantId, chargeId).ToArrayAsync(cancellationToken);
        var payments = await PaymentQuery(db, tenantId, chargeId).ToArrayAsync(cancellationToken);
        var settled = await db.PaymentSettlements.AsNoTracking().AnyAsync(
            paymentSettlement => paymentSettlement.TenantId == tenantId &&
                paymentSettlement.ChargeId == chargeId,
            cancellationToken);
        return new(charge.TenantId, charge.ChargeId, charge.Amount, items, payments, settled);
    }

    internal static IQueryable<ChargeSettlementItem> ItemQuery(YallaFinanceDbContext db, Guid tenantId, Guid chargeId)
        => db.Database.SqlQuery<ChargeSettlementItem>($"""
            SELECT item.tenant_id AS tenant_id, item.charge_id AS charge_id, item.id AS id, item.ordinal AS ordinal,
                   item.amount AS amount, item.provider AS provider, item.provider_external_id AS provider_external_id,
                   source.id AS financial_connection_id
            FROM finance.charge_item item
            LEFT JOIN core.external_connection source ON source.tenant_id = item.tenant_id
                AND source.enabled AND source."group" = 'FinancialSystem' AND source.provider = item.provider
            WHERE item.tenant_id = {tenantId} AND item.charge_id = {chargeId} ORDER BY item.ordinal, item.id
            """);

    internal static IQueryable<ChargeSettlementChargeRow> ChargeQuery(
        YallaFinanceDbContext db,
        Guid tenantId,
        Guid chargeId)
        => db.Database.SqlQuery<ChargeSettlementChargeRow>($"""
            SELECT tenant_id AS tenant_id, id AS charge_id, amount AS amount
            FROM finance.charge WHERE tenant_id = {tenantId} AND id = {chargeId}
            """);

    internal static IQueryable<ChargeSettlementPayment> PaymentQuery(
        YallaFinanceDbContext db,
        Guid tenantId,
        Guid chargeId)
        => db.Database.SqlQuery<ChargeSettlementPayment>($"""
            SELECT p.tenant_id AS tenant_id, p.charge_id AS charge_id, p.id AS id,
                   p.connection_id AS bank_connection_id, a.payment_method_id AS payment_method_id,
                   p.amount AS amount, p.received_at AS received_at
            FROM finance.payment_transaction p
            JOIN finance.payment_attempt a ON a.tenant_id = p.tenant_id AND a.id = p.payment_attempt_id
            WHERE p.tenant_id = {tenantId} AND p.charge_id = {chargeId}
            ORDER BY p.received_at, p.id
            """);

    public async Task<ChargeSettlementCheckpoint?> ReadPreviousAsync(
        IJobExecutionContext execution,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        if (!IsValidJobExecutionContext(execution))
            throw new InvalidOperationException("Job execution identity is invalid.");
        var rows = await PreviousCheckpointQuery(db, execution, chargeId).ToArrayAsync(cancellationToken);
        if (rows.Length == 0)
            return null;
        var checkpoint = JsonSerializer.Deserialize<ChargeSettlementCheckpoint>(
            rows[0],
            JsonSerializerOptions.Web);
        return checkpoint ?? throw new JsonException("Settlement checkpoint is empty.");
    }

    internal static IQueryable<string> PreviousCheckpointQuery(
        YallaFinanceDbContext db,
        IJobExecutionContext execution,
        Guid chargeId)
    {
        var key = $"charge:{chargeId:D}";
        return db.Database.SqlQuery<string>($"""
            SELECT e.details::text AS "Value"
            FROM core.job_execution e JOIN core.job j ON j.tenant_id = e.tenant_id AND j.id = e.job_id
            WHERE e.tenant_id = {execution.TenantId} AND e.job_id = {execution.JobId} AND e.id <> {execution.ExecutionId}
              AND j.type = {SettleChargeJob.JobType}
              AND j.key = {key}
              AND e.details ? 'schemaVersion'
            ORDER BY e.started_at DESC, e.id DESC LIMIT 1
            """);
    }

    public Task SaveAsync(
        IJobExecutionContext execution,
        ChargeSettlementCheckpoint checkpoint,
        int expectedRevision,
        CancellationToken cancellationToken)
        => SaveCoreAsync(execution, checkpoint, expectedRevision, cancellationToken);

    public async Task CompleteChargeAsync(
        Guid tenantId,
        Guid chargeId,
        Guid representativePaymentTransactionId,
        IReadOnlyList<Guid> contributingPaymentTransactionIds,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || chargeId == Guid.Empty || representativePaymentTransactionId == Guid.Empty
            || contributingPaymentTransactionIds is not { Count: > 0 }
            || contributingPaymentTransactionIds.Any(id => id == Guid.Empty)
            || contributingPaymentTransactionIds.Distinct().Count() != contributingPaymentTransactionIds.Count)
            throw new ArgumentException("Settlement completion identity is invalid.");
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, clock);
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                var scope = $"charge-settlement:{tenantId:D}:{chargeId:D}";
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))",
                    token);
                var currentUtcTime = audit.UtcNow;
                var covered = await db.Database.SqlQuery<bool>($"""
                SELECT c.amount <= SUM(p.amount)
                    AND COUNT(DISTINCT p.id) = cardinality({contributingPaymentTransactionIds})
                    AND {representativePaymentTransactionId} = ANY({contributingPaymentTransactionIds}) AS "Value"
                FROM finance.charge c JOIN finance.payment_transaction p
                  ON p.tenant_id = c.tenant_id AND p.charge_id = c.id
                 AND p.id = ANY({contributingPaymentTransactionIds})
                WHERE c.tenant_id = {tenantId} AND c.id = {chargeId}
                GROUP BY c.tenant_id, c.id, c.amount
                """).SingleOrDefaultAsync(token);
                if (!covered)
                    throw new InvalidOperationException("Persisted receipts do not cover the charge.");
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                INSERT INTO finance.payment_settlement (id, tenant_id, charge_id, payment_transaction_id, settled_at, created_at, created_by, updated_at, updated_by)
                SELECT {Guid.NewGuid()}, {tenantId}, {chargeId}, {representativePaymentTransactionId}, {currentUtcTime}, {audit.UtcNow}, {audit.ActorId}, NULL, NULL
                WHERE EXISTS (SELECT 1 FROM finance.payment_transaction p WHERE p.tenant_id = {tenantId}
                    AND p.charge_id = {chargeId} AND p.id = {representativePaymentTransactionId})
                ON CONFLICT (tenant_id, charge_id) DO NOTHING
                """,
                    token);
                var settlement = await db.Database.SqlQuery<Guid>($"""
                SELECT payment_transaction_id AS "Value" FROM finance.payment_settlement
                WHERE tenant_id = {tenantId} AND charge_id = {chargeId}
                """).SingleOrDefaultAsync(token);
                if (settlement == Guid.Empty)
                    throw new InvalidOperationException("The charge settlement marker was not persisted.");
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                UPDATE finance.debt d SET status = 'Paid', updated_at = {audit.UtcNow}, updated_by = {audit.ActorId}
                FROM finance.charge_item i
                WHERE i.tenant_id = {tenantId} AND i.charge_id = {chargeId} AND d.tenant_id = i.tenant_id
                  AND d.id = i.debt_id AND d.status IN ('Open', 'InCollection')
                """,
                    token);
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                UPDATE finance.payment_request r SET status = 'Completed', completed_at = {currentUtcTime},
                    completion_reason = CASE WHEN EXISTS (
                        SELECT 1 FROM finance.payment_attempt a JOIN finance.payment_transaction p
                          ON p.tenant_id = a.tenant_id AND p.payment_attempt_id = a.id
                        WHERE a.tenant_id = r.tenant_id AND a.payment_request_id = r.id
                          AND p.id = ANY({contributingPaymentTransactionIds})
                    ) THEN 'RequestPayment' ELSE 'ChargeSettledElsewhere' END,
                    updated_at = {audit.UtcNow}, updated_by = {audit.ActorId}
                WHERE r.tenant_id = {tenantId} AND r.charge_id = {chargeId} AND r.status = 'Active'
                """,
                    token);
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                return true;
            },
            cancellationToken);
    }

    private async Task SaveCoreAsync(
        IJobExecutionContext execution,
        ChargeSettlementCheckpoint checkpoint,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        if (!IsValidJobExecutionContext(execution) ||
            checkpoint.Revision != expectedRevision + 1 ||
            checkpoint.ChargeId == Guid.Empty)
            throw new ArgumentException("Settlement checkpoint identity is invalid.");
        var json = JsonSerializer.Serialize(checkpoint, JsonSerializerOptions.Web);
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, clock);
        var key = $"charge:{checkpoint.ChargeId:D}";
        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE core.job_execution e SET details = CAST({json} AS jsonb), updated_at = {audit.UtcNow}, updated_by = {audit.ActorId}
            FROM core.job j
            WHERE e.tenant_id = {execution.TenantId} AND e.job_id = {execution.JobId} AND e.id = {execution.ExecutionId}
              AND e.finished_at IS NULL AND j.tenant_id = e.tenant_id AND j.id = e.job_id
              AND j.type = {SettleChargeJob.JobType} AND j.key = {key} AND j.status = 1
              AND (({expectedRevision} = 0 AND e.details IS NULL)
                   OR e.details ->> 'revision' = CAST({expectedRevision} AS text))
            """,
            cancellationToken);
        if (affected != 1)
            throw new InvalidOperationException("The open Job execution changed while saving settlement progress.");
    }

    private static bool IsValidJobExecutionContext(IJobExecutionContext execution)
        => execution.TenantId != Guid.Empty &&
            execution.JobId != Guid.Empty &&
            execution.ExecutionId != Guid.Empty;

}
