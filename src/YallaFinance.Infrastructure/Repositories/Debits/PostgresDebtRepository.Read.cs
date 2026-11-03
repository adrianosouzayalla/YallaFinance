using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;


using YallaFinance.Domain.Debts;
using YallaFinance.Infrastructure.Database.Generated;
using Debt = YallaFinance.Infrastructure.Database.Generated.Entities.Debt;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Debits;

namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed partial class PostgresDebtRepository
{
    private static readonly Expression<Func<Debt, DebtResponse>> Projection = DebtResponseProjection.Select;

    private IQueryable<Debt> BuildTenantDebtQuery(string tenantId)
    {
        var tenantGuid = Guid.TryParse(tenantId, out var parsedTenantGuid) ? parsedTenantGuid : Guid.Empty;
        return db.Debts.AsNoTracking().Where(debt => debt.TenantId == tenantGuid
            && debt.Contract.TenantId == tenantGuid && debt.Contract.Customer.TenantId == tenantGuid
            && debt.Category.TenantId == tenantGuid);
    }

    public Task<DebtResponse?> GetAsync(string tenantId, string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var debtId))
            return Task.FromResult<DebtResponse?>(null);
        return BuildTenantDebtQuery(tenantId).Where(debt => debt.Id == debtId).Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DebtListResult> ListAsync(
        string tenantId,
        DebtFiltersRequest filters,
        CancellationToken cancellationToken)
    {
        var query = BuildTenantDebtQuery(tenantId);
        var categoryValues = filters.Categories?.Where(categoryValue => !string.IsNullOrWhiteSpace(categoryValue)).ToArray() ?? [];
        if (categoryValues.Length > 0)
        {
            var categoryIds = categoryValues
                .Where(categoryValue => Guid.TryParse(categoryValue, out _))
                .Select(Guid.Parse)
                .ToArray();
            query = query.Where(debt => categoryIds.Contains(debt.CategoryId) && debt.Category.Active);
        }
        if (filters.Status is { } debtStatus)
        {
            var debtStatusName = debtStatus.ToString();
            query = query.Where(debt => debt.Status == debtStatusName);
        }
        if (filters.StartDate is { } startDate)
            query = query.Where(debt => debt.OccurrenceDate >= startDate);
        if (filters.EndDate is { } endDate)
            query = query.Where(debt => debt.OccurrenceDate <= endDate);

        var totals = await query.GroupBy(debt => debt.Status)
            .Select(group => new { Status = group.Key, Count = group.Count(), Amount = group.Sum(debt => debt.Amount) })
            .ToListAsync(cancellationToken);
        var openDebtTotals = totals.FirstOrDefault(statusTotals => statusTotals.Status == "Open");
        var summary = new DebtSummary(openDebtTotals?.Count ?? 0, openDebtTotals?.Amount ?? 0,
            totals.FirstOrDefault(statusTotals => statusTotals.Status == "InCollection")?.Amount ?? 0,
            totals.FirstOrDefault(statusTotals => statusTotals.Status == "Paid")?.Amount ?? 0);
        var ascending = filters.SortDirection == "asc";
        IOrderedQueryable<Debt> ordered = filters.SortBy?.ToLowerInvariant() switch
        {
            "amount" => ascending ? query.OrderBy(debt => debt.Amount) : query.OrderByDescending(debt => debt.Amount),
            "occurrencedate" => ascending ? query.OrderBy(debt => debt.OccurrenceDate) : query.OrderByDescending(debt => debt.OccurrenceDate),
            "tenantname" => ascending ? query.OrderBy(debt => debt.Contract.Customer.Name) : query.OrderByDescending(debt => debt.Contract.Customer.Name),
            _ => query.OrderBy(debt => debt.Id),
        };
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 1000);
        var offset = ((long)page - 1) * pageSize;
        var items = offset > int.MaxValue ? [] : await ordered.ThenBy(debt => debt.Id)
            .Skip((int)offset).Take(pageSize).Select(Projection).ToArrayAsync(cancellationToken);
        return new DebtListResult(items, totals.Sum(statusTotals => statusTotals.Count), page, pageSize, summary);
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        // Preserve the current dashboard's UTC date boundary.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var metrics = await BuildTenantDebtQuery(tenantId).GroupBy(debt => 1).Select(group => new
        {
            OpenCount = group.Count(debt => debt.Status == "Open"),
            OpenAmount = group.Sum(debt => debt.Status == "Open" ? debt.Amount : 0),
            ChargingCount = group.Count(debt => debt.Status == "InCollection"),
            ChargingAmount = group.Sum(debt => debt.Status == "InCollection" ? debt.Amount : 0),
            OverdueCount = group.Count(debt => debt.Status == "Open" && debt.DueDate < today),
            OverdueAmount = group.Sum(debt => debt.Status == "Open" && debt.DueDate < today ? debt.Amount : 0),
        }).FirstOrDefaultAsync(cancellationToken);
        return new DashboardSummaryResponse(new(metrics?.OpenCount ?? 0, metrics?.OpenAmount ?? 0),
            new(metrics?.ChargingCount ?? 0, metrics?.ChargingAmount ?? 0),
            new(metrics?.OverdueCount ?? 0, metrics?.OverdueAmount ?? 0), new(0, 0));
    }
}
