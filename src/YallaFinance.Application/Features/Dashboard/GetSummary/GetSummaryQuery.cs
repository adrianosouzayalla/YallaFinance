using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Dashboard.GetSummary;

public sealed record GetSummaryQuery : IRequest<NotificationResult<DashboardSummaryResponse>>;
