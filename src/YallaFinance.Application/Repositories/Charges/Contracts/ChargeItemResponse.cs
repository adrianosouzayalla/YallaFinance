using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts;

public sealed record ChargeItemResponse(
    string Id,
    string DebtId,
    string CategoryId,
    decimal Amount,
    int Ordinal,
    string? Provider,
    string? ProviderExternalId,
    CreateChargeResult? FinancialIntegration = null,
    ChargeItemDebtResponse? Debt = null,
    string? ExternalCostCenterId = null,
    string? CostCenterName = null,
    string? ExactAmount = null);
