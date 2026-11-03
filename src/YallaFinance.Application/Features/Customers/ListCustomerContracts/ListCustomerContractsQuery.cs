using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Customers.ListCustomerContracts;

public sealed record ListCustomerContractsQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20,
    DateOnly? OccurrenceDate = null) : IRequest<NotificationResult<CustomerContractPage>>;
