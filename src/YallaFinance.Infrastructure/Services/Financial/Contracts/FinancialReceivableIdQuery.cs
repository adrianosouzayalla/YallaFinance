namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialReceivableIdQuery(
    Guid TenantId,
    string Provider,
    string ProviderExternalId,
    Guid? ConnectionId = null);
