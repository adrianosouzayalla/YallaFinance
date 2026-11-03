using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Results;

using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Application.Repositories.CustomerContracts;
using YallaFinance.Application.Features.Customers.ListCustomerContracts;
using YallaFinance.Application.Repositories.Fleet;

namespace YallaFinance.Infrastructure.Repositories.CustomerContracts;

public sealed partial class PostgresContractRepository
{
    public async Task<NotificationResult<CustomerContractPage>> ListAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken token,
        DateOnly? occurrenceDate = null)
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(
            customer => customer.Id == customerId &&
                customer.TenantId == tenantId &&
                customer.Active &&
                customer.Tenant.Active,
            token))
            return NotificationResult<CustomerContractPage>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Cliente indisponível."));
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
            return NotificationResult<CustomerContractPage>.Success(new(
                [],
                page,
                pageSize,
                false));
        var query = db.Contracts.AsNoTracking().Where(contract => contract.TenantId == tenantId &&
            contract.CustomerId == customerId &&
            contract.Active);
        if (occurrenceDate is { } date)
        {
            var history = await movements.FindAsync(tenantId.ToString(), customerId.ToString(), date, token);
            var numbers = history.Where(x => x.TenantId == tenantId.ToString() &&
                x.CustomerId == customerId.ToString() &&
                x.OccurrenceDate == date)
                .Select(x => x.Contract).Distinct().ToArray();
            query = query.Where(contract => numbers.Contains(contract.Number));
        }
        var items = await query
            .OrderBy(contract => contract.Number).ThenBy(contract => contract.Id).Skip((int)offset).Take(pageSize + 1)
            .Select(contract => new CustomerContractResponse(
                contract.Id,
                contract.Number,
                contract.Plate,
                contract.Product,
                contract.Fleet)).ToArrayAsync(token);
        return NotificationResult<CustomerContractPage>.Success(new(
            items.Take(pageSize).ToArray(),
            page,
            pageSize,
            items.Length > pageSize));
    }
}
