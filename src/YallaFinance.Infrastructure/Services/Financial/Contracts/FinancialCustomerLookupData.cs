namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialCustomerLookupData(
    Guid TenantId,
    string Provider,
    string Document,
    Guid? ConnectionId = null);
