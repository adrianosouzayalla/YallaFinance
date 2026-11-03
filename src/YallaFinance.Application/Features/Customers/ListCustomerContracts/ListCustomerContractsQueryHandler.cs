using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.CustomerContracts;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Customers.ListCustomerContracts;

public sealed class ListCustomerContractsQueryHandler(
    ICurrentTenantContext context,
    ICustomerContractRepository queries)
    : IRequestHandler<ListCustomerContractsQuery, NotificationResult<CustomerContractPage>>
{
    public Task<NotificationResult<CustomerContractPage>> Handle(
        ListCustomerContractsQuery request,
        CancellationToken token)
    {
        if (!Guid.TryParse(context.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return Task.FromResult(NotificationResult<CustomerContractPage>.Failure(new NotificationError(
                ErrorCodes.Forbidden,
                "Sessão sem tenant ativo.")));
        if (request.CustomerId == Guid.Empty ||
            request.Page < 1 ||
            request.PageSize is < 1 or > 20 ||
            request.OccurrenceDate == DateOnly.MinValue)
            return Task.FromResult(NotificationResult<CustomerContractPage>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Confira cliente e paginação.")));
        return queries.ListAsync(
            tenantId,
            request.CustomerId,
            request.Page,
            request.PageSize,
            token,
            request.OccurrenceDate);
    }
}
