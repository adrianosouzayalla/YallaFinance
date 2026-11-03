using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Infrastructure.Database.Generated;
using E = YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Repositories.Charges;

internal sealed class PostgresChargeWriter(
    YallaFinanceDbContext db,
    PostgresChargeReadOperations charges,
    PostgresChargeItemRepository items)
{
    public Task AcquireIdempotencyLockAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        EnsureActiveTransaction();
        var lockKey = $"{tenantId}:{idempotencyKey}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public Task AddCreationAsync(ChargeCreationData charge, CancellationToken cancellationToken)
    {
        EnsureActiveTransaction();
        cancellationToken.ThrowIfCancellationRequested();

        var persistedCharge = new E.Charge
        {
            Id = charge.Id,
            TenantId = charge.TenantId,
            Amount = charge.Amount,
            DueDate = charge.DueDate,
            Note = charge.Note,
            Source = charge.Source,
            IdempotencyKey = charge.IdempotencyKey,
            RequestHash = charge.RequestHash,
            CreatedAt = charge.CreatedAt
        };
        charges.Add(persistedCharge);

        var persistedItems = charge.Items.Select(item => new E.ChargeItem
        {
            Id = item.Id,
            TenantId = charge.TenantId,
            ChargeId = charge.Id,
            DebtId = item.DebtId,
            Amount = item.Amount,
            Ordinal = item.Ordinal
        }).ToArray();
        items.AddRange(persistedItems);
        foreach (var (persistedItem, creationItem) in persistedItems.Zip(charge.Items))
            items.SetCostCenter(persistedItem, creationItem.ExternalCostCenterId, creationItem.CostCenterName);

        return Task.CompletedTask;
    }

    private void EnsureActiveTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("An active unit of work is required.");
    }
}
