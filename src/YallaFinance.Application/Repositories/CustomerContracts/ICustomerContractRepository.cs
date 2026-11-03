using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Customers.ListCustomerContracts;

namespace YallaFinance.Application.Repositories.CustomerContracts;

public interface ICustomerContractRepository
{
    Task<NotificationResult<CustomerContractPage>> ListAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken token,
        DateOnly? occurrenceDate = null);
    Task AcquireInboundContractNumberLockAsync(Guid tenantId, string contractNumber, CancellationToken cancellationToken);
    Task AcquireContractLockAsync(Guid tenantId, string contractNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChargeContractRecord>> FindForChargeAsync(
        Guid tenantId,
        string contractNumber,
        CancellationToken cancellationToken);
    Task AddForChargeAsync(ChargeContractData contract, CancellationToken cancellationToken);
    Task UpdateForChargeAsync(ChargeContractData contract, CancellationToken cancellationToken);
}

public sealed record ChargeContractRecord(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    string Number,
    string? Plate,
    string? Product,
    string? Fleet,
    bool Active);

public sealed record ChargeContractData(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    string Number,
    string? Plate,
    string? Product,
    string? Fleet,
    bool Active,
    DateTime CreatedAt,
    DateTime UpdatedAt);
