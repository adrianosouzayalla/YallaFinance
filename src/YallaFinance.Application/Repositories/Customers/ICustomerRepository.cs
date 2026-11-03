using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Customers;

public interface ICustomerRepository
{
    Task<CustomerProfileData?> FindProfileAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    // Stages changes in the caller's unit of work; does not independently commit a business transaction.
    Task<bool> UpdateProfileAsync(Guid tenantId, Guid customerId, CustomerProfileUpdate update, CancellationToken cancellationToken);
    public const int MinimumSearchLength = 3;
    public const int MaximumResults = 20;
    Task<IReadOnlyList<CustomerResponse>> SearchAsync(
        string tenantId,
        string term,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerResponse>> SearchExternalAsync(
        string tenantId,
        string term,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerResponse>> SuggestAsync(
        string tenantId,
        string search,
        CancellationToken cancellationToken,
        bool persistedOnly = false);
    Task<ChargeCustomerRecord?> FindLocalForChargeAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ChargeCustomerRecord>> FindLocalByDocumentForChargeAsync(
        Guid tenantId,
        string normalizedDocument,
        CancellationToken cancellationToken);
    Task AcquireCustomerLockAsync(Guid customerId, CancellationToken cancellationToken);
    Task AcquireDocumentLockAsync(Guid tenantId, string normalizedDocument, CancellationToken cancellationToken);
    Task AddForChargeAsync(ChargeCustomerData customer, CancellationToken cancellationToken);
    Task UpdateForChargeAsync(ChargeCustomerData customer, CancellationToken cancellationToken);
}

public sealed record ChargeCustomerRecord(
    Guid Id,
    Guid TenantId,
    string Name,
    string Document,
    string DocumentType,
    string? Email,
    string? Phone,
    bool Active,
    DateTime CreatedAt);

public sealed record ChargeCustomerData(
    Guid Id,
    Guid TenantId,
    string Name,
    string Document,
    string DocumentType,
    string? Email,
    string? Phone,
    bool Active,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    YallaFinance.Domain.Customers.CustomerAddressData? Address = null);
