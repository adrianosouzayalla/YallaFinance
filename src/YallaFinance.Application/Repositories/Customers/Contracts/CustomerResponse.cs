
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Customers.Contracts;

public sealed record CustomerResponse(
    string Id,
    string Name,
    string TaxId,
    string Phone,
    string Email,
    string? TenantId = null,
    string? CustomerId = null);
