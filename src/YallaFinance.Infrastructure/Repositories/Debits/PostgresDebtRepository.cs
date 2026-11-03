using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Fleet;
using YallaFinance.Infrastructure.Database.Generated;
using Debt = YallaFinance.Infrastructure.Database.Generated.Entities.Debt;

namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed partial class PostgresDebtRepository : IDebtRepository
{
    private readonly YallaFinanceDbContext db;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider clock;
    private readonly IFleetMovementRepository movements;
    private readonly YallaFinance.Application.Common.Context.CurrentExecutionActorContext actorContext;

    public PostgresDebtRepository(
        YallaFinanceDbContext db,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        IFleetMovementRepository movements,
        YallaFinance.Application.Common.Context.CurrentExecutionActorContext actorContext)
    {
        this.db = db;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.movements = movements;
        this.actorContext = actorContext;
    }

    public void Add(Debt debt) => db.Debts.Add(debt);

    public async Task<IReadOnlyList<ChargeDebtRecord>> LockAndLoadForChargeAsync(
        Guid tenantId,
        IReadOnlyList<Guid> debtIds,
        CancellationToken cancellationToken)
    {
        if (debtIds.Count == 0)
            return [];

        var orderedDebtIds = debtIds.Order().ToArray();
        await db.Debts.FromSqlInterpolated(
                $"SELECT * FROM finance.debt WHERE tenant_id = {tenantId} AND id = ANY({orderedDebtIds}) ORDER BY id FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        var persistedDebts = await db.Debts
            .Include(debt => debt.Contract)
            .ThenInclude(contract => contract.Customer)
            .Include(debt => debt.Category)
            .Where(debt => debt.TenantId == tenantId && orderedDebtIds.Contains(debt.Id))
            .OrderBy(debt => debt.Id)
            .ToArrayAsync(cancellationToken);
        foreach (var persistedDebt in persistedDebts)
            await db.Entry(persistedDebt).ReloadAsync(cancellationToken);

        return persistedDebts.Select(debt => new ChargeDebtRecord(
            debt.Id,
            debt.TenantId,
            debt.Contract.CustomerId,
            debt.Contract.TenantId,
            debt.Contract.Active,
            debt.Contract.Customer.TenantId,
            debt.Contract.Customer.Active,
            debt.Category.TenantId,
            debt.Category.Active,
            debt.Status,
            debt.Amount,
            debt.Category.Name)).ToArray();
    }

    public Task AddForChargeAsync(ChargeDebtData debt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        db.Debts.Add(new Debt
        {
            Id = debt.Id,
            TenantId = debt.TenantId,
            ContractId = debt.ContractId,
            CategoryId = debt.CategoryId,
            Amount = debt.Amount,
            OccurrenceDate = debt.OccurrenceDate,
            DueDate = debt.DueDate,
            Description = debt.Description,
            Status = debt.Status,
            Origin = debt.Origin,
            CreatedAt = debt.CreatedAt,
            UpdatedAt = debt.UpdatedAt
        });
        return Task.CompletedTask;
    }

    public Task MarkInCollectionAsync(
        Guid tenantId,
        IReadOnlyList<Guid> debtIds,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var persistedDebts = db.Debts.Local
            .Where(debt => debt.TenantId == tenantId && debtIds.Contains(debt.Id))
            .ToArray();
        foreach (var persistedDebt in persistedDebts)
        {
            persistedDebt.Status = "InCollection";
            persistedDebt.UpdatedAt = updatedAt;
        }
        return Task.CompletedTask;
    }
}
