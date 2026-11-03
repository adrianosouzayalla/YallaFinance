using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Customers.SuggestCustomers;

public sealed record SuggestCustomersQuery(string Search, string? Source = null) : IRequest<NotificationResult<IReadOnlyList<CustomerResponse>>>;
