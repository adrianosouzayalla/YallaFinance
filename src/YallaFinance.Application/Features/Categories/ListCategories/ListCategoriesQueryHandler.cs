using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application.Repositories.Categories;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Categories.ListCategories;

public sealed class ListCategoriesQueryHandler(
    ICategoryRepository repository,
    ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<ListCategoriesQuery, NotificationResult<IReadOnlyList<CategoryResponse>>>
{
    public async Task<NotificationResult<IReadOnlyList<CategoryResponse>>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<IReadOnlyList<CategoryResponse>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));

        return NotificationResult<IReadOnlyList<CategoryResponse>>.Success(await repository.ListAsync(
            currentTenant.TenantId,
            cancellationToken));

    }
}
