namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialCustomer(
    string ProviderExternalId,
    string Name,
    string? Document,
    bool IsDeleted,
    bool IsArchived,
    string? Email = null,
    string? Phone = null,
    YallaFinance.Domain.Customers.CustomerAddressData? Address = null);
