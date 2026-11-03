using YallaFinance.Application;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application.Features.Charges.CreateDirectCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Application.Repositories.Charges;

public interface IChargeRepository
{
    Task AcquireIdempotencyLockAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);
    Task<ChargeIdempotencyRecord?> FindByIdempotencyKeyAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken);
    Task<bool> ContainsAnyDebtAsync(Guid tenantId, IReadOnlyList<Guid> debtIds, CancellationToken cancellationToken);
    Task AddCreationAsync(ChargeCreationData charge, CancellationToken cancellationToken);
    Task<ChargeDetailsResult> AddAsync(ChargeDetailsResult charge, CancellationToken cancellationToken);
    Task<ChargeDetailsResult?> GetAsync(string tenantId, string chargeId, CancellationToken cancellationToken);
    Task<ChargeSettlementSnapshot?> LoadAsync(Guid tenantId, Guid chargeId, CancellationToken cancellationToken);
    Task<ChargeSettlementCheckpoint?> ReadPreviousAsync(
        IJobExecutionContext execution,
        Guid chargeId,
        CancellationToken cancellationToken);
    Task SaveAsync(
        IJobExecutionContext execution,
        ChargeSettlementCheckpoint checkpoint,
        int expectedRevision,
        CancellationToken cancellationToken);
    Task CompleteChargeAsync(
        Guid tenantId,
        Guid chargeId,
        Guid representativePaymentTransactionId,
        IReadOnlyList<Guid> contributingPaymentTransactionIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ChargeItemIntegrationRequest>> GetPendingAsync(
        string tenantId,
        string chargeId,
        CancellationToken cancellationToken);
    Task RecordProviderResultAsync(
        string tenantId,
        Guid itemId,
        string? provider,
        string? externalId,
        CancellationToken cancellationToken);
}

public sealed record ChargeIdempotencyRecord(Guid Id, string Source, string RequestHash);

public sealed record ChargeCreationData(
    Guid Id,
    Guid TenantId,
    decimal Amount,
    DateOnly DueDate,
    string? Note,
    string Source,
    string IdempotencyKey,
    string RequestHash,
    DateTime CreatedAt,
    IReadOnlyList<ChargeCreationItemData> Items);

public sealed record ChargeCreationItemData(
    Guid Id,
    Guid DebtId,
    decimal Amount,
    int Ordinal,
    string? ExternalCostCenterId = null,
    string? CostCenterName = null);
