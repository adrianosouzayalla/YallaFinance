using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Customers;
namespace YallaFinance.Infrastructure.Repositories.Customers;

public sealed partial class PostgresCustomerRepository
{
    public Task<IReadOnlyList<CustomerResponse>> SearchExternalAsync(
        string tenantId,
        string term,
        CancellationToken cancellationToken) =>
        // Compatibility name: the official customer source is now exclusively the database.
        SearchAsync(tenantId, term, cancellationToken);

    public async Task<IReadOnlyList<CustomerResponse>> SuggestAsync(
        string tenantId,
        string search,
        CancellationToken cancellationToken,
        bool persistedOnly = false)
    {
        search = search.Trim();
        if (!Guid.TryParse(tenantId, out var id) ||
            search.Length < ICustomerRepository.MinimumSearchLength)
            return [];
        var local = await db.Customers.AsNoTracking()
            .Where(customer => customer.TenantId == id && customer.Active).Where(CustomerSearchPredicate.Build(search))
            .OrderBy(customer => customer.Name).ThenBy(customer => customer.Id).Take(ICustomerRepository.MaximumResults)
            .Select(customer => new CustomerResponse(
                customer.Id.ToString(),
                customer.Name,
                customer.Document,
                customer.Phone ?? "",
                customer.Email ?? "",
                customer.TenantId.ToString(),
                customer.Id.ToString()))
            .ToArrayAsync(cancellationToken);
        return local;
    }

    public async Task<IReadOnlyList<CustomerResponse>> SearchAsync(
        string tenantId,
        string term,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var id))
            return [];
        var search = term?.Trim() ?? "";
        var query = db.Customers.AsNoTracking().Where(customer => customer.TenantId == id && customer.Active);
        if (search.Length > 0)
            query = query.Where(CustomerSearchPredicate.Build(search));
        var local = await query.OrderBy(customer => customer.Name).ThenBy(customer => customer.Id).Select(customer => new CustomerResponse(
            customer.Id.ToString(),
            customer.Name,
            customer.Document,
            customer.Phone ?? "",
            customer.Email ?? "",
            customer.TenantId.ToString(),
            customer.Id.ToString())).ToArrayAsync(cancellationToken);
        return local;
    }

}
