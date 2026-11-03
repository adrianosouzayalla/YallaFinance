using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using ChargeItem = YallaFinance.Infrastructure.Database.Generated.Entities.ChargeItem;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Repositories.Charges;

namespace YallaFinance.Infrastructure.Repositories.Charges;

internal sealed class PostgresChargeItemRepository(YallaFinanceDbContext db,
    YallaFinance.Application.Common.Context.CurrentExecutionActorContext actorContext, TimeProvider clock)
{
    internal void AddRange(IEnumerable<ChargeItem> items) => db.ChargeItems.AddRange(items);

    public Task<bool> ContainsAnyDebtAsync(
        Guid tenantId,
        IReadOnlyList<Guid> debtIds,
        CancellationToken cancellationToken) =>
        db.ChargeItems.AnyAsync(
            item => item.TenantId == tenantId && debtIds.Contains(item.DebtId),
            cancellationToken);

    internal void SetCostCenter(ChargeItem item, string? externalCostCenterId, string? costCenterName)
    {
        db.Entry(item).Property<string?>("ExternalCostCenterId").CurrentValue = externalCostCenterId;
        db.Entry(item).Property<string?>("CostCenterName").CurrentValue = costCenterName;
    }

    public async Task<IReadOnlyList<ChargeItemIntegrationRequest>> GetPendingAsync(
        string tenantId,
        string chargeId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var tenant) || !Guid.TryParse(chargeId, out var charge))
            return [];
        var items = await db.ChargeItems.AsNoTracking().Include(value => value.Charge)
            .Include(value => value.Debt).ThenInclude(value => value.Contract).ThenInclude(value => value.Customer)
                .ThenInclude(value => value.CustomerAddress)
            .Where(value => value.TenantId == tenant && value.ChargeId == charge && value.ProviderExternalId == null
                && value.Charge.TenantId == tenant && value.Debt.TenantId == tenant
                && value.Debt.Contract.TenantId == tenant && value.Debt.Contract.Customer.TenantId == tenant)
            .OrderBy(value => value.Ordinal).ToArrayAsync(cancellationToken);
        return items.Select(CreatePendingChargeItemIntegrationRequest).ToArray();
    }

    internal static ChargeItemIntegrationRequest CreatePendingChargeItemIntegrationRequest(ChargeItem chargeItem)
    {
        var customer = chargeItem.Debt.Contract.Customer;
        return new ChargeItemIntegrationRequest(
            chargeItem.Id,
            new CreateChargeData(
                chargeItem.Id,
                chargeItem.TenantId,
                new(customer.Id, customer.Name, customer.Document, customer.Email, customer.Phone,
                    customer.CustomerAddress is not { } address ? null : new(
                        address.CountryCode, address.PostalCode, address.Street, address.Number, address.WithoutNumber,
                        address.Complement, address.District, address.City, address.StateCode),
                    customer.PhoneCountryCallingCode is null || customer.PhoneNationalNumber is null ? null
                        : new(customer.PhoneCountryCallingCode, customer.PhoneNationalNumber)),
                chargeItem.Debt.CategoryId,
                chargeItem.Amount,
                chargeItem.Charge.DueDate,
                chargeItem.Charge.DueDate,
                chargeItem.Debt.OccurrenceDate,
                chargeItem.Debt.Description,
                ExpectedProvider: chargeItem.Provider,
                ExternalCostCenterId: chargeItem.ExternalCostCenterId),
            chargeItem.Provider);
    }

    public async Task RecordProviderResultAsync(
        string tenantId,
        Guid itemId,
        string? provider,
        string? externalId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var tenant))
            throw new ArgumentException(
                "Invalid tenant.",
                nameof(tenantId));
        if (provider is null)
        {
            if (externalId is not null)
                throw new ArgumentException("An external ID requires a resolved provider.");
            return;
        }
        if (string.IsNullOrWhiteSpace(provider) ||
            externalId is not null &&
            string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("Provider identifiers cannot be blank.");
        var audit = YallaFinance.Infrastructure.Database.Auditing.AuditWriteMetadata.Capture(actorContext, clock);
        var updated = await db.ChargeItems.Where(value => value.TenantId == tenant && value.Id == itemId
            && (value.Provider == null || value.Provider == provider)
            && (value.ProviderExternalId == null || value.ProviderExternalId == externalId)
            && (value.Provider != provider || value.ProviderExternalId != externalId))
            .ExecuteUpdateAsync(
                update => update.SetProperty(value => value.Provider, provider)
                    .SetProperty(value => value.ProviderExternalId, externalId)
                    .SetProperty(value => value.UpdatedAt, audit.UtcNow)
                    .SetProperty(value => value.UpdatedBy, audit.ActorId),
                cancellationToken);
        if (updated == 0 && await db.ChargeItems.AsNoTracking().AnyAsync(value => value.TenantId == tenant
                && value.Id == itemId && value.Provider == provider && value.ProviderExternalId == externalId, cancellationToken))
            return;
        if (updated != 1)
            throw new InvalidOperationException("Charge item not found or provider identity conflicts.");
    }
}
