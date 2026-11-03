using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Customers;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Customers.SearchCustomers;

public sealed class SearchCustomersQueryHandler(
    ICustomerRepository repository,
    ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<SearchCustomersQuery, NotificationResult<IReadOnlyList<CustomerResponse>>>
{
    public async Task<NotificationResult<IReadOnlyList<CustomerResponse>>> Handle(
        SearchCustomersQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<IReadOnlyList<CustomerResponse>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        return NotificationResult<IReadOnlyList<CustomerResponse>>.Success(await repository.SearchAsync(
            currentTenant.TenantId!,
            request.Search,
            cancellationToken));
    }
}
