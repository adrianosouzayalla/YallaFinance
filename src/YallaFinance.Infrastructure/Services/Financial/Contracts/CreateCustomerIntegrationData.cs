namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record CreateCustomerIntegrationData(
    Guid CustomerId,
    Guid TenantId,
    string Name,
    string Provider,
    Guid? ConnectionId = null,
    string? Document = null,
    string? Email = null,
    string? Phone = null,
    YallaFinance.Domain.Customers.CustomerAddressData? Address = null);
