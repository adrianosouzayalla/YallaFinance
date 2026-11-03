using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Debits.ListDebits;

public sealed record ListDebitsQuery(DebtFiltersRequest Filters) : IRequest<NotificationResult<DebtListResult>>;
