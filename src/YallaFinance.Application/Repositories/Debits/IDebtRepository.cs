using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Debits.CreateManualDebit;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;


namespace YallaFinance.Application.Repositories.Debits;

public interface IDebtRepository
{
    Task<DebtListResult> ListAsync(
        string tenantId,
        DebtFiltersRequest filters,
        CancellationToken cancellationToken);
    Task<DebtResponse?> GetAsync(string tenantId, string id, CancellationToken cancellationToken);
    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        string tenantId,
        CancellationToken cancellationToken);
    Task<NotificationResult<CreateManualDebitResult>> CreateManualAsync(
        Guid tenantId,
        CreateManualDebitCommand request,
        CancellationToken cancellationToken);
    Task<NotificationResult<ReceiveDebitResult>> ReceiveAsync(
        Guid tenantId,
        ReceiveDebitPayload payload,
        CancellationToken cancellationToken);
    Task<ManualDebtRequest?> FindManualRequestAsync(Guid tenantId, Guid key, CancellationToken cancellationToken);
    Task LockManualRequestAsync(Guid tenantId, Guid key, CancellationToken cancellationToken);
    Task AddManualRequestAsync(ManualDebtRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChargeDebtRecord>> LockAndLoadForChargeAsync(
        Guid tenantId,
        IReadOnlyList<Guid> debtIds,
        CancellationToken cancellationToken);
    Task AddForChargeAsync(ChargeDebtData debt, CancellationToken cancellationToken);
    Task MarkInCollectionAsync(Guid tenantId, IReadOnlyList<Guid> debtIds, DateTime updatedAt, CancellationToken cancellationToken);
}

public sealed record ChargeDebtRecord(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    Guid ContractTenantId,
    bool ContractActive,
    Guid CustomerTenantId,
    bool CustomerActive,
    Guid CategoryTenantId,
    bool CategoryActive,
    string Status,
    decimal Amount,
    string CategoryName);

public sealed record ChargeDebtData(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    Guid CategoryId,
    decimal Amount,
    DateOnly OccurrenceDate,
    DateOnly DueDate,
    string Description,
    string Status,
    string Origin,
    DateTime CreatedAt,
    DateTime UpdatedAt);
