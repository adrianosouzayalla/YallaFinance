using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Customers.SearchCustomers;

public sealed record SearchCustomersQuery(string Search) : IRequest<NotificationResult<IReadOnlyList<CustomerResponse>>>;
