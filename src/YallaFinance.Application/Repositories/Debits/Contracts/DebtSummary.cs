
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Debits.Contracts;

public sealed record DebtSummary(
    int OpenCount,
    decimal OpenAmount,
    decimal ChargingAmount,
    decimal PaidAmount);
