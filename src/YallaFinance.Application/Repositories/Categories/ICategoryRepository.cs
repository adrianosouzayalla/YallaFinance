using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryResponse>> FindActiveByIdsAsync(
        Guid tenantId,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken);
}
