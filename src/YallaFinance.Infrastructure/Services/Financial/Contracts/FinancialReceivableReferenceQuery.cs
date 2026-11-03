namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialReceivableReferenceQuery(
    Guid TenantId,
    string Provider,
    Guid ChargeId,
    Guid? ConnectionId = null,
    int PageSize = 100);
