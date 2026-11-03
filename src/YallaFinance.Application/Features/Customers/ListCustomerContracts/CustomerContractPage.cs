using YallaFinance.Application;
namespace YallaFinance.Application.Features.Customers.ListCustomerContracts;

public sealed record CustomerContractPage(
    IReadOnlyList<CustomerContractResponse> Items,
    int Page,
    int PageSize,
    bool HasMore);
