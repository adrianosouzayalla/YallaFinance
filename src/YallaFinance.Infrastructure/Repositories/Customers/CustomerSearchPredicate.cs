using System.Linq.Expressions;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Domain.Customers;
namespace YallaFinance.Infrastructure.Repositories.Customers;

public static class CustomerSearchPredicate
{
    public static Expression<Func<Customer, bool>> Build(string term)
    {
        var search = term.Trim().ToLowerInvariant();
        var canonical = CustomerDocument.Normalize(term);
        return customer => customer.Name.ToLower().Contains(search) || customer.Document.Contains(search)
            || (canonical != "" && customer.NormalizedDocument != null && customer.NormalizedDocument.Contains(canonical))
            || (customer.Email != null && customer.Email.ToLower().Contains(search));
    }
}
