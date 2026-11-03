using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Financial.Contracts;


namespace YallaFinance.Infrastructure.Repositories.Charges;

internal sealed class PostgresChargeRepository(
    PostgresChargeWriter writer,
    PostgresChargeReadOperations persisted,
    PostgresChargeSettlementStore settlements,
    PostgresChargeItemRepository items) : IChargeRepository
{
    public Task AcquireIdempotencyLockAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        writer.AcquireIdempotencyLockAsync(tenantId, idempotencyKey, cancellationToken);

    public Task<ChargeIdempotencyRecord?> FindByIdempotencyKeyAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        persisted.FindByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken);

    public Task<bool> ContainsAnyDebtAsync(
        Guid tenantId,
        IReadOnlyList<Guid> debtIds,
        CancellationToken cancellationToken) =>
        items.ContainsAnyDebtAsync(tenantId, debtIds, cancellationToken);

    public Task AddCreationAsync(ChargeCreationData charge, CancellationToken cancellationToken) =>
        writer.AddCreationAsync(charge, cancellationToken);

    public Task<ChargeDetailsResult> AddAsync(ChargeDetailsResult charge, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Charges must use the transactional writer.");
    }

    public async Task<ChargeDetailsResult?> GetAsync(
        string tenantId,
        string chargeId,
        CancellationToken cancellationToken) =>
        await persisted.GetAsync(tenantId, chargeId, cancellationToken);

    public Task<ChargeSettlementSnapshot?> LoadAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken) => settlements.LoadAsync(tenantId, chargeId, cancellationToken);

    public Task<ChargeSettlementCheckpoint?> ReadPreviousAsync(
        IJobExecutionContext execution,
        Guid chargeId,
        CancellationToken cancellationToken) => settlements.ReadPreviousAsync(execution, chargeId, cancellationToken);

    public Task SaveAsync(
        IJobExecutionContext execution,
        ChargeSettlementCheckpoint checkpoint,
        int expectedRevision,
        CancellationToken cancellationToken) => settlements.SaveAsync(execution, checkpoint, expectedRevision, cancellationToken);

    public Task CompleteChargeAsync(
        Guid tenantId,
        Guid chargeId,
        Guid representativePaymentTransactionId,
        IReadOnlyList<Guid> contributingPaymentTransactionIds,
        CancellationToken cancellationToken) => settlements.CompleteChargeAsync(
            tenantId,
            chargeId,
            representativePaymentTransactionId,
            contributingPaymentTransactionIds,
            cancellationToken);

    public Task<IReadOnlyList<ChargeItemIntegrationRequest>> GetPendingAsync(
        string tenantId,
        string chargeId,
        CancellationToken cancellationToken) => items.GetPendingAsync(tenantId, chargeId, cancellationToken);

    public Task RecordProviderResultAsync(
        string tenantId,
        Guid itemId,
        string? provider,
        string? externalId,
        CancellationToken cancellationToken) => items.RecordProviderResultAsync(
            tenantId,
            itemId,
            provider,
            externalId,
            cancellationToken);

}
