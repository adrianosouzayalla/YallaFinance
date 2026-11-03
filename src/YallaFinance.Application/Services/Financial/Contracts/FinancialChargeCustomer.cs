using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record FinancialChargeCustomer(
    Guid CustomerId,
    string Name,
    string? Document = null,
    string? Email = null,
    string? Phone = null,
    YallaFinance.Domain.Customers.CustomerAddressData? Address = null,
    YallaFinance.Domain.Customers.CustomerPhoneData? StructuredPhone = null);
