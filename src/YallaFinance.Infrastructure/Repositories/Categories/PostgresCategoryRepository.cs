using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application.Repositories.Categories;

namespace YallaFinance.Infrastructure.Repositories.Categories;

public sealed class PostgresCategoryRepository(YallaFinanceDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryResponse>> FindActiveByIdsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .Where(category => category.TenantId == tenantId
                && category.Active
                && categoryIds.Contains(category.Id))
            .Select(category => new CategoryResponse(
                category.Id.ToString(),
                category.Code,
                category.Name,
                category.TenantId.ToString(),
                category.Active))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var id))
            return [];
        return await db.Categories.AsNoTracking()
            .Where(category => category.TenantId == id && category.Active)
            .OrderBy(category => category.Id)
            .Select(category => new CategoryResponse(category.Id.ToString(), category.Code,
                category.Name, category.TenantId.ToString(), category.Active))
            .ToArrayAsync(cancellationToken);
    }
}
