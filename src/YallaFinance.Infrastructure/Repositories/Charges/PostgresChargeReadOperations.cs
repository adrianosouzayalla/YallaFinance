using Microsoft.EntityFrameworkCore;
using System.Globalization;
using YallaFinance.Application.Features.Charges.CreateCharge;


using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Debts;
using YallaFinance.Infrastructure.Database.Generated;
using Charge = YallaFinance.Infrastructure.Database.Generated.Entities.Charge;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Repositories.Charges;

namespace YallaFinance.Infrastructure.Repositories.Charges;

internal sealed class PostgresChargeReadOperations(YallaFinanceDbContext db)
{
    internal void Add(Charge charge) => db.Charges.Add(charge);

    internal Task<Charge?> FindByKeyAsync(Guid tenantId, string key, CancellationToken cancellationToken) =>
        db.Charges.AsNoTracking().FirstOrDefaultAsync(
            value => value.TenantId == tenantId &&
                value.IdempotencyKey == key,
            cancellationToken);

    public async Task<ChargeIdempotencyRecord?> FindByIdempotencyKeyAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var charge = await FindByKeyAsync(tenantId, idempotencyKey, cancellationToken);
        return charge is null
            ? null
            : new ChargeIdempotencyRecord(charge.Id, charge.Source, charge.RequestHash);
    }

    public async Task<ChargeDetailsResult?> GetAsync(
        string tenantId,
        string chargeId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var tenant) || !Guid.TryParse(chargeId, out var id))
            return null;
        var charge = await db.Charges.AsNoTracking()
            .Include(value => value.Tenant)
            .Include(value => value.ChargeItems).ThenInclude(value => value.Debt).ThenInclude(value => value.Contract)
                .ThenInclude(value => value.Customer)
            .FirstOrDefaultAsync(value => value.TenantId == tenant && value.Id == id, cancellationToken);
        if (charge is null)
            return null;
        var items = charge.ChargeItems.OrderBy(value => value.Ordinal).ToArray();
        var costCenters = await db.ChargeItems.AsNoTracking().Where(item => item.TenantId == tenant && item.ChargeId == id)
            .Select(item => new { item.Id, ExternalId = EF.Property<string>(item, "ExternalCostCenterId"), Name = EF.Property<string>(item, "CostCenterName") })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        if (items.Length == 0)
            throw new InvalidOperationException("Persisted charge has no items.");
        if (items.Any(value => value.TenantId != tenant ||
            value.Debt.TenantId != tenant ||
            value.Debt.Contract.TenantId != tenant || value.Debt.Contract.Customer.TenantId != tenant)
            || items.Select(value => value.Debt.Contract.CustomerId).Distinct().Count() != 1)
            throw new InvalidOperationException("Persisted charge relationships are inconsistent.");
        var confirmed = items.All(value => value.Provider is not null && value.ProviderExternalId is not null);
        var integration = new CreateChargeResult(
            confirmed ? FinancialIntegrationOutcome.Completed : FinancialIntegrationOutcome.Indeterminate,
            null,
            null,
            null,
            false,
            confirmed ? null : "IntegrationPendingOrUnconfirmed");
        return new ChargeDetailsResult(
            charge.Id.ToString(),
            charge.IdempotencyKey,
            Enum.Parse<ChargeSource>(charge.Source),
            tenantId,
            items[0].Debt.Contract.CustomerId.ToString(),
            items.Select(value => value.DebtId.ToString())
                .ToArray(),
            charge.Amount,
            items.Select(value => value.Debt.CategoryId).Distinct().Count() == 1 ? items[0].Debt.CategoryId.ToString() : "",
            charge.DueDate,
            "",
            "",
            "",
            new DateTimeOffset(DateTime.SpecifyKind(
                charge.CreatedAt,
                DateTimeKind.Utc)),
            integration,
            Items: items.Select(value => new ChargeItemResponse(
                value.Id.ToString(),
                value.DebtId.ToString(),
                value.Debt.CategoryId.ToString(),
                value.Amount,
                value.Ordinal,
                value.Provider,
                value.ProviderExternalId,
                Debt: new ChargeItemDebtResponse(
                    value.Debt.Contract.Number,
                    value.Debt.Contract.Plate,
                    value.Debt.Contract.Product,
                    value.Debt.Contract.Fleet,
                    value.Debt.OccurrenceDate,
                    value.Debt.DueDate,
                    value.Debt.Description,
                    value.Debt.Origin),
                ExternalCostCenterId: costCenters[value.Id].ExternalId,
                CostCenterName: costCenters[value.Id].Name,
                ExactAmount: value.Amount.ToString("0.00", CultureInfo.InvariantCulture))).ToArray(),
            Note: charge.Note,
            Reference: ChargeReference.Format(charge.Tenant.Code, charge.Number),
            ExactAmount: charge.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            CustomerName: items[0].Debt.Contract.Customer.Name);
    }
}
