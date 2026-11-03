using YallaFinance.Infrastructure.Database.Transactions;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Results;



using YallaFinance.Infrastructure.Database.Generated;
using E = YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Features.Debits.CreateManualDebit;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Fleet;
namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed partial class PostgresDebtRepository
{
    public async Task<NotificationResult<CreateManualDebitResult>> CreateManualAsync(
        Guid tenantId,
        CreateManualDebitCommand request,
        CancellationToken token)
    {
        var fingerprint = ManualDebitFingerprint.Compute(request);
        if (!await db.Tenants.AsNoTracking().AnyAsync(x => x.Id == tenantId && x.Active, token))
            return CreateManualDebitFailureResult(ErrorCodes.Forbidden, "Tenant indisponível.");
        try
        {
            return await unitOfWork.ExecuteAsync(
                async cancellation =>
                {
                    await LockManualRequestAsync(tenantId, request.IdempotencyKey, cancellation);
                    var prior = await FindManualRequestAsync(tenantId, request.IdempotencyKey, cancellation);
                    if (prior is not null)
                        return CreateManualDebtReplayResult(prior, fingerprint);
                    // Lock referenced rows so concurrent deactivation cannot invalidate an accepted new write.
                    var tenant = await db.Tenants.FromSqlInterpolated($"SELECT * FROM core.tenant WHERE id={tenantId} AND active FOR SHARE")
                        .SingleOrDefaultAsync(cancellation);
                    if (tenant is null)
                        return CreateManualDebitFailureResult(ErrorCodes.Forbidden, "Tenant indisponível.");
                    var customer = await db.Customers.FromSqlInterpolated($"SELECT * FROM finance.customer WHERE id={request.CustomerId} AND tenant_id={tenantId} AND active FOR SHARE")
                        .SingleOrDefaultAsync(cancellation);
                    var contract = await db.Contracts.FromSqlInterpolated($"SELECT * FROM finance.contract WHERE id={request.ContractId} AND tenant_id={tenantId} AND active FOR SHARE")
                        .SingleOrDefaultAsync(cancellation);
                    var category = await db.Categories.FromSqlInterpolated($"SELECT * FROM finance.category WHERE id={request.CategoryId} AND tenant_id={tenantId} AND active FOR SHARE")
                        .SingleOrDefaultAsync(cancellation);
                    if (customer is null ||
                        contract is null ||
                        contract.CustomerId != customer.Id ||
                        category is null)
                        return CreateManualDebitFailureResult(
                            ErrorCodes.Validation,
                            "Cliente, contrato ou categoria indisponível. Atualize a seleção.");
                    var history = await movements.FindAsync(
                        tenantId.ToString(),
                        customer.Id.ToString(),
                        request.OccurrenceDate,
                        cancellation);
                    if (!history.Any(movement => movement.TenantId == tenantId.ToString()
                        && movement.CustomerId == customer.Id.ToString() && movement.OccurrenceDate == request.OccurrenceDate
                        && movement.Contract == contract.Number && movement.Plate == (contract.Plate ?? "")
                        && movement.Product == (contract.Product ?? "") && movement.Fleet == (contract.Fleet ?? "")))
                        return CreateManualDebitFailureResult(
                            ErrorCodes.Validation,
                            "Contrato/veículo não corresponde à movimentação do cliente na data da ocorrência.");
                    var currentUtcTime = clock.GetUtcNow();
                    var debt = new E.Debt
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ContractId = contract.Id,
                        CategoryId = category.Id,
                        Amount = request.Amount,
                        OccurrenceDate = request.OccurrenceDate,
                        DueDate = request.DueDate,
                        Description = request.Description,
                        Status = "Open",
                        Origin = "Yalla Finance",
                        CreatedAt = currentUtcTime.UtcDateTime,
                        UpdatedAt = currentUtcTime.UtcDateTime
                    };
                    db.Debts.Add(debt);
                    // FK is deferred: SaveChanges and SQL receipt insert are committed together by UoW.
                    await AddManualRequestAsync(
                        new(tenantId, request.IdempotencyKey, fingerprint, debt.Id, currentUtcTime),
                        cancellation);
                    PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellation));
                    return NotificationResult<CreateManualDebitResult>.Success(new(debt.Id, true));
                },
                token);
        }
        catch (ManualDebtRequestConcurrencyException)
        {
            var winner = await FindManualRequestAsync(tenantId, request.IdempotencyKey, token);
            if (winner is null)
                throw new InvalidOperationException("Confirmed manual debt receipt could not be reloaded.");
            return CreateManualDebtReplayResult(winner, fingerprint);
        }
    }

    private static NotificationResult<CreateManualDebitResult> CreateManualDebtReplayResult(
        ManualDebtRequest prior,
        string fingerprint) =>
        prior.RequestFingerprint == fingerprint ? NotificationResult<CreateManualDebitResult>.Success(new(
            prior.DebtId,
            false))
            : CreateManualDebitFailureResult(ErrorCodes.Conflict, "A tentativa já foi registrada com outros dados.");

    private static NotificationResult<CreateManualDebitResult> CreateManualDebitFailureResult(
        string code,
        string message) =>
        NotificationResult<CreateManualDebitResult>.Failure(new NotificationError(code, message));
}
