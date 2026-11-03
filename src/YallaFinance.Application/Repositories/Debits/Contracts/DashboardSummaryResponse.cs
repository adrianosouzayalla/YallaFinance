
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record DashboardSummaryResponse(
    DashboardMetric AwaitingCharge,
    DashboardMetric ActiveCharges,
    DashboardMetric Overdue,
    DashboardMetric StaleCharges);
