using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Debits.GetDebit;

public sealed record GetDebitQuery(string Id) : IRequest<NotificationResult<DebtResponse>>;
