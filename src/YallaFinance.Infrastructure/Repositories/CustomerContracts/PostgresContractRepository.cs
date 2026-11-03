using Microsoft.EntityFrameworkCore;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Application.Repositories.CustomerContracts;
using YallaFinance.Application.Repositories.Fleet;

namespace YallaFinance.Infrastructure.Repositories.CustomerContracts;

public sealed partial class PostgresContractRepository : ICustomerContractRepository
{
    private readonly YallaFinanceDbContext db;
    private readonly IFleetMovementRepository movements;

    public PostgresContractRepository(YallaFinanceDbContext db, IFleetMovementRepository movements)
    {
        this.db = db;
        this.movements = movements;
    }

    // The demo API supplies a contract number, not a contract external identifier.
    public Task<Contract[]> FindByNumberAsync(Guid tenant, string number, CancellationToken cancellationToken) =>
        db.Contracts.Where(value => value.TenantId == tenant && value.Number == number)
            .ToArrayAsync(cancellationToken);

    public void Add(Contract contract) => db.Contracts.Add(contract);

    public Task AcquireInboundContractNumberLockAsync(
        Guid tenantId,
        string contractNumber,
        CancellationToken cancellationToken)
    {
        var lockKey = GetInboundContractNumberLockKey(tenantId, contractNumber);
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    internal static string GetInboundContractNumberLockKey(Guid tenantId, string contractNumber) =>
        $"inbound-contract-number:{tenantId}:{contractNumber}";

    public Task AcquireContractLockAsync(Guid tenantId, string contractNumber, CancellationToken cancellationToken)
    {
        var lockKey = $"contract:{tenantId}:{contractNumber}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public async Task<IReadOnlyList<ChargeContractRecord>> FindForChargeAsync(
        Guid tenantId,
        string contractNumber,
        CancellationToken cancellationToken) =>
        await db.Contracts.AsNoTracking()
            .Where(contract => contract.TenantId == tenantId && contract.Number == contractNumber)
            .Select(contract => new ChargeContractRecord(
                contract.Id,
                contract.TenantId,
                contract.CustomerId,
                contract.Number,
                contract.Plate,
                contract.Product,
                contract.Fleet,
                contract.Active))
            .ToArrayAsync(cancellationToken);

    public Task AddForChargeAsync(ChargeContractData contract, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        db.Contracts.Add(new Contract
        {
            Id = contract.Id,
            TenantId = contract.TenantId,
            CustomerId = contract.CustomerId,
            Number = contract.Number,
            Plate = contract.Plate,
            Product = contract.Product,
            Fleet = contract.Fleet,
            Active = contract.Active,
        });
        return Task.CompletedTask;
    }

    public async Task UpdateForChargeAsync(ChargeContractData contract, CancellationToken cancellationToken)
    {
        var persistedContract = await db.Contracts.SingleAsync(
            value => value.TenantId == contract.TenantId && value.Id == contract.Id,
            cancellationToken);
        persistedContract.CustomerId = contract.CustomerId;
        persistedContract.Number = contract.Number;
        persistedContract.Plate = contract.Plate;
        persistedContract.Product = contract.Product;
        persistedContract.Fleet = contract.Fleet;
        persistedContract.Active = contract.Active;
    }
}
