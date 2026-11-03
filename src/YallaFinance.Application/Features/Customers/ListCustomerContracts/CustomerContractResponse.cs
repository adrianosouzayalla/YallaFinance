using YallaFinance.Application;
namespace YallaFinance.Application.Features.Customers.ListCustomerContracts;

public sealed record CustomerContractResponse(
    Guid Id,
    string Number,
    string? Plate,
    string? Product,
    string? Fleet);
