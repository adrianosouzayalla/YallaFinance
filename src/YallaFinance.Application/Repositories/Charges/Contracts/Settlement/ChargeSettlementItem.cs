using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementItem(
    Guid TenantId,
    Guid ChargeId,
    Guid Id,
    int Ordinal,
    decimal Amount,
    string? Provider,
    string? ProviderExternalId,
    Guid? FinancialConnectionId = null);
