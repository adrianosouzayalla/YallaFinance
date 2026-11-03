using Microsoft.EntityFrameworkCore;


using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application.Repositories.PaymentMethods;

namespace YallaFinance.Infrastructure.Repositories.PaymentMethods;

public sealed class PostgresPaymentMethodRepository(YallaFinanceDbContext db) : IPaymentMethodRepository
{
    public async Task<IReadOnlyList<PaymentMethodResponse>> ListAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var id))
            return [];
        return await db.PaymentMethods.AsNoTracking()
            .Where(method => method.TenantId == id && method.Active)
            .OrderBy(method => method.Id)
            .Select(method => new PaymentMethodResponse(method.Id.ToString(), method.Code,
                method.Name, method.TenantId.ToString(), method.Active, method.InstrumentType, method.InternalDescription))
            .ToArrayAsync(cancellationToken);
    }
}
