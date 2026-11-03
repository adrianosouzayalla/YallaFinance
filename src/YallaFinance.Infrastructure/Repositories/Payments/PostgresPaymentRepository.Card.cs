using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Infrastructure.Database.Transactions;
using YallaFinance.Domain.Customers;

namespace YallaFinance.Infrastructure.Repositories.Payments;

// Same repository/responsibility; card persistence is separated from the PIX-specific code for readability.
public sealed partial class PostgresPaymentRepository
{
    public async Task<CardCheckoutContext?> GetCardCheckoutContextAsync(string token, string methodCode, CancellationToken ct)
    {
        var request = await db.PaymentRequests.AsNoTracking().SingleOrDefaultAsync(item => item.TokenHash == token, ct);
        if (request is null) return null;
        var authorized = await db.PaymentRequestMethods.AsNoTracking().AnyAsync(item => item.TenantId == request.TenantId
            && item.PaymentRequestId == request.Id && item.PaymentMethod.Code == methodCode
            && item.PaymentMethod.InstrumentType == "CreditCard", ct);
        if (!authorized) return null;
        var customer = await ReadCardCustomerAsync(request.TenantId, request.ChargeId, ct);
        var charge = await db.Charges.AsNoTracking().SingleAsync(item => item.TenantId == request.TenantId && item.Id == request.ChargeId, ct);
        return customer is null ? null : new(request.TenantId, request.ChargeId, charge.DueDate, customer);
    }

    private async Task<CustomerProfileData?> ReadCardCustomerAsync(Guid tenantId, Guid chargeId, CancellationToken ct)
    {
        var customerIds = await db.ChargeItems.AsNoTracking().Where(item => item.TenantId == tenantId && item.ChargeId == chargeId)
            .Select(item => item.Debt.Contract.CustomerId).Distinct().Take(2).ToArrayAsync(ct);
        if (customerIds.Length != 1) return null;
        var customer = await db.Customers.AsNoTracking().Include(item => item.CustomerAddress)
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == customerIds[0], ct);
        if (customer is null) return null;
        var address = customer.CustomerAddress;
        return new(customer.Id, tenantId, customer.Name, customer.Document, customer.DocumentType,
            customer.DocumentIssuingCountry, customer.Email, customer.Phone,
            customer.PhoneCountryCallingCode is null ? null : new CustomerPhoneData(customer.PhoneCountryCallingCode, customer.PhoneNationalNumber!),
            address is null ? null : new CustomerAddressRecord(address.Id, new CustomerAddressData(address.CountryCode,
                address.PostalCode, address.Street, address.Number, address.WithoutNumber, address.Complement,
                address.District, address.City, address.StateCode)));
    }

    public async Task<CardAttemptState?> GetCardAttemptAsync(Guid tenantId, Guid attemptId, CancellationToken ct) =>
        await db.PaymentAttemptCards.AsNoTracking().Where(item => item.TenantId == tenantId && item.PaymentAttemptId == attemptId)
            .Select(item => new CardAttemptState(item.ProviderCustomerId, item.ProviderChargeId, item.ProviderStatus, item.PaymentDispatchedAt))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CardPaymentReconciliationCandidate>> ListCardPaymentReconciliationCandidatesAsync(
        Guid tenantId,
        Guid connectionId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || connectionId == Guid.Empty || limit is < 1 or > 100 || offset < 0)
            return [];

        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        var recoveryCutoff = currentUtcTime - TimeSpan.FromMinutes(2);
        var candidates = await (
            from cardAttempt in db.PaymentAttemptCards.AsNoTracking()
            join paymentAttempt in db.PaymentAttempts.AsNoTracking()
                on new { cardAttempt.TenantId, Id = cardAttempt.PaymentAttemptId }
                equals new { paymentAttempt.TenantId, paymentAttempt.Id }
            where cardAttempt.TenantId == tenantId
                && cardAttempt.ConnectionId == connectionId
                && cardAttempt.ProviderChargeId != null
                && cardAttempt.PaymentDispatchedAt != null
                && paymentAttempt.IssuanceBlocked
                && (paymentAttempt.Status == "Unknown"
                    || paymentAttempt.Status == "AwaitingPayment" && paymentAttempt.ExpiresAt == null
                    || paymentAttempt.Status == "Processing"
                        && (paymentAttempt.UpdatedAt ?? paymentAttempt.CreatedAt) <= recoveryCutoff)
                && !db.PaymentTransactions.Any(paymentTransaction =>
                    paymentTransaction.TenantId == tenantId
                    && paymentTransaction.ConnectionId == connectionId
                    && paymentTransaction.PaymentAttemptId == paymentAttempt.Id)
            orderby paymentAttempt.Id
            select new
            {
                PaymentAttempt = paymentAttempt,
                cardAttempt.ProviderChargeId
            })
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return candidates.Select(candidate => new CardPaymentReconciliationCandidate(
            ToPaymentAttemptData(candidate.PaymentAttempt, YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard),
            candidate.ProviderChargeId!)).ToArray();
    }

    public Task SaveCardIdentityAsync(PaymentAttemptData attempt, string? customerId, string? chargeId,
        string? providerStatus, CancellationToken ct) => unitOfWork.ExecuteAsync(async token =>
    {
        await LockChargeForProviderAttemptAsync(attempt, token);
        if (await db.PaymentAttempts.AsNoTracking().AnyAsync(item => item.TenantId == attempt.TenantId
            && item.Id == attempt.Id && (item.Status == "Succeeded" || item.Status == "Failed" || item.Status == "Canceled"), token)) return true;
        var card = await db.PaymentAttemptCards.SingleAsync(item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, token);
        if (card.ConnectionId != attempt.ConnectionId || card.ProviderCustomerId is not null && card.ProviderCustomerId != customerId
            || card.ProviderChargeId is not null && card.ProviderChargeId != chargeId)
            throw new InvalidOperationException("Card identity changed during processing.");
        card.ProviderCustomerId = customerId;
        card.ProviderChargeId = chargeId;
        card.ProviderStatus = providerStatus;
        PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
        return true;
    }, ct);

    public Task<bool> TryMarkCardDispatchedAsync(PaymentAttemptData attempt, CancellationToken ct) => unitOfWork.ExecuteAsync(async token =>
    {
        await LockChargeForProviderAttemptAsync(attempt, token);
        var card = await db.PaymentAttemptCards.SingleAsync(item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, token);
        // Reload tracked rows after acquiring the lock; a previous call may already have dispatched.
        await db.Entry(card).ReloadAsync(token);
        var parent = await db.PaymentAttempts.AsNoTracking().SingleAsync(item => item.TenantId == attempt.TenantId && item.Id == attempt.Id, token);
        if (card.PaymentDispatchedAt is not null || card.ProviderChargeId is null || card.ProviderCustomerId is null
            || !parent.IssuanceBlocked || parent.Status != "Processing") return false;
        card.PaymentDispatchedAt = clock.GetUtcNow().UtcDateTime;
        PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
        return true;
    }, ct);

    public Task PersistCardResultAsync(PaymentAttemptData attempt, CreditCardCharge charge, CancellationToken ct) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            await LockChargeForProviderAttemptAsync(attempt, token);
            var parent = await db.PaymentAttempts.SingleAsync(item => item.TenantId == attempt.TenantId && item.Id == attempt.Id, token);
            await db.Entry(parent).ReloadAsync(token);
            var card = await db.PaymentAttemptCards.SingleAsync(item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, token);
            await db.Entry(card).ReloadAsync(token);
            if (parent.ConnectionId != attempt.ConnectionId || parent.Provider != attempt.Provider || parent.Amount != charge.Amount
                || charge.Reference != attempt.ProviderExternalId || card.ProviderChargeId != charge.Id || card.ProviderCustomerId != charge.CustomerId)
                throw new InvalidOperationException("Provider card result does not match the reserved attempt.");
            card.ProviderStatus = charge.ProviderStatus;
            // Never downgrade a receipt that another reconciliation has already confirmed.
            if (parent.Status == "Succeeded") return true;
            var confirmed = charge.State is CreditCardPaymentState.Confirmed or CreditCardPaymentState.Received;
            if (!confirmed && parent.Status is "Failed" or "Canceled") return true;
            if (confirmed)
            {
                var receipt = await db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item => item.TenantId == attempt.TenantId
                    && item.ConnectionId == attempt.ConnectionId && item.ProviderTransactionId == charge.Id, token);
                if (receipt is not null && (receipt.PaymentAttemptId != attempt.Id || receipt.ChargeId != attempt.ChargeId || receipt.Amount != charge.Amount))
                    throw new InvalidOperationException("Provider card receipt conflicts with an existing payment.");
                if (receipt is null)
                    db.PaymentTransactions.Add(new PaymentTransaction
                    {
                        Id = Guid.NewGuid(), TenantId = attempt.TenantId, ChargeId = attempt.ChargeId,
                        PaymentAttemptId = attempt.Id, ConnectionId = attempt.ConnectionId,
                        ProviderTransactionId = charge.Id, Amount = charge.Amount,
                        ReceivedAt = clock.GetUtcNow().UtcDateTime, ProviderPayload = charge.ProviderPayloadJson
                    });
                parent.Status = "Succeeded";
                parent.IssuanceBlocked = false;
                parent.RiskClosedAt = clock.GetUtcNow().UtcDateTime;
                parent.RiskClosureEvidence = "ProviderConfirmedCardPayment";
                parent.FailureCode = null;
                parent.FailureMessage = null;
                await FlushPaymentChangesAsync(token);
                if (!await HasSettlementJobAsync(attempt.TenantId, attempt.ChargeId, token))
                    await (jobs ?? throw new InvalidOperationException("JobService is required to persist card payments."))
                        .CreateAsync(attempt.TenantId, new SettleChargeJob(attempt.ChargeId), token);
            }
            else if (charge.Deleted)
            {
                parent.Status = "Failed";
                parent.IssuanceBlocked = false;
                parent.RiskClosedAt = clock.GetUtcNow().UtcDateTime;
                parent.RiskClosureEvidence = "ProviderConfirmedCardCancellation";
                parent.FailureCode = "CardDeclined";
                parent.FailureMessage = "Cartão recusado; cobrança remota cancelada e encerramento confirmado.";
            }
            else
            {
                parent.Status = "AwaitingPayment";
                parent.IssuanceBlocked = true;
                parent.ExpiresAt = null;
                parent.FailureMessage = "Aguardando confirmação do pagamento com cartão.";
            }
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
            return true;
        }, ct);

    private async Task LockChargeForProviderAttemptAsync(PaymentAttemptData attempt, CancellationToken ct)
    {
        // Receipt writers acquire this advisory lock before inserting FKs to the charge.
        // Keep that order to avoid a charge-row/advisory-lock inversion with PIX settlement.
        await LockChargeForReceiptAsync(attempt.TenantId, attempt.ChargeId, ct);
        var rows = await db.Charges.FromSqlInterpolated(
            $"SELECT * FROM finance.charge WHERE tenant_id = {attempt.TenantId} AND id = {attempt.ChargeId} FOR UPDATE")
            .AsNoTracking().ToArrayAsync(ct);
        if (rows.Length != 1) throw new InvalidOperationException("Card charge not found.");
    }

    public Task<CardAttemptFailureResult> RecordCardAttemptFailureAsync(
        PaymentAttemptData attempt,
        string failureCode,
        string failureMessage,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            await LockChargeForProviderAttemptAsync(attempt, token);
            var parent = await db.PaymentAttempts.SingleAsync(item => item.TenantId == attempt.TenantId && item.Id == attempt.Id, token);
            await db.Entry(parent).ReloadAsync(token);
            var card = await db.PaymentAttemptCards.SingleAsync(
                item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id,
                token);
            await db.Entry(card).ReloadAsync(token);
            if (parent.Status is "Succeeded" or "Failed" or "Canceled" or "Expired")
                return new CardAttemptFailureResult(parent.Status, parent.Status == "Failed" && !parent.IssuanceBlocked,
                    parent.FailureMessage);

            parent.FailureCode = SafeFailureCode(failureCode) ?? "CardOutcomeUnknown";
            parent.FailureMessage = SafeFailureMessage(failureMessage)
                ?? "Não foi possível confirmar o resultado do pagamento com cartão.";
            if (card.PaymentDispatchedAt is null)
            {
                parent.Status = "Failed";
                parent.IssuanceBlocked = false;
                parent.RiskClosedAt = clock.GetUtcNow().UtcDateTime;
                parent.RiskClosureEvidence = "CardPaymentNotDispatched";
            }
            else
            {
                parent.Status = "Unknown";
                parent.IssuanceBlocked = true;
                parent.RiskClosedAt = null;
                parent.RiskClosureEvidence = null;
            }
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
            return new CardAttemptFailureResult(parent.Status, parent.Status == "Failed" && !parent.IssuanceBlocked,
                parent.FailureMessage);
        }, ct);
}
