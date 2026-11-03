namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialCatalogQuery(
    Guid TenantId,
    string Provider,
    Guid? ConnectionId = null,
    int Skip = 0,
    int Take = 100);
