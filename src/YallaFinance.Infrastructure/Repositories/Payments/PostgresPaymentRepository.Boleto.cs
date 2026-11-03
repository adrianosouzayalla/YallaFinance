using Microsoft.EntityFrameworkCore;
using System.Data;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Infrastructure.Database.Transactions;

namespace YallaFinance.Infrastructure.Repositories.Payments;

public sealed partial class PostgresPaymentRepository
{
    public async Task<BoletoAttemptState?> GetBoletoAttemptAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken) =>
        await db.PaymentAttemptBoletos.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.PaymentAttemptId == attemptId)
            .Select(item => new BoletoAttemptState(item.ProviderPaymentId, item.ProviderStatus))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BoletoPaymentReconciliationCandidate>> ListBoletoPaymentReconciliationCandidatesAsync(
        Guid tenantId,
        Guid connectionId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || connectionId == Guid.Empty || limit is < 1 or > 100 || offset < 0)
            return [];

        var recoveryCutoff = clock.GetUtcNow().UtcDateTime - TimeSpan.FromMinutes(2);
        var candidates = await (
            from boletoAttempt in db.PaymentAttemptBoletos.AsNoTracking()
            join paymentAttempt in db.PaymentAttempts.AsNoTracking()
                on new { boletoAttempt.TenantId, Id = boletoAttempt.PaymentAttemptId }
                equals new { paymentAttempt.TenantId, paymentAttempt.Id }
            where boletoAttempt.TenantId == tenantId
                && boletoAttempt.ConnectionId == connectionId
                && paymentAttempt.IssuanceBlocked
                && (paymentAttempt.Status == "Unknown"
                    || paymentAttempt.Status == "AwaitingPayment"
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
                boletoAttempt.ProviderPaymentId
            })
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return candidates.Select(candidate => new BoletoPaymentReconciliationCandidate(
            ToPaymentAttemptData(candidate.PaymentAttempt, YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto),
            candidate.ProviderPaymentId)).ToArray();
    }

    public Task PersistBoletoResultAsync(
        PaymentAttemptData attempt,
        BoletoCharge charge,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        await LockChargeForProviderAttemptAsync(attempt, token);
        var parent = await db.PaymentAttempts.SingleAsync(
            item => item.TenantId == attempt.TenantId && item.Id == attempt.Id,
            token);
        await db.Entry(parent).ReloadAsync(token);
        var boleto = await db.PaymentAttemptBoletos.SingleAsync(
            item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id,
            token);
        await db.Entry(boleto).ReloadAsync(token);

        if (parent.ConnectionId != attempt.ConnectionId || parent.Provider != attempt.Provider
            || parent.Amount != charge.Amount || charge.ProviderReference != attempt.ProviderExternalId
            || boleto.ConnectionId != attempt.ConnectionId
            || boleto.ProviderPaymentId is not null && boleto.ProviderPaymentId != charge.ProviderPaymentId)
            throw new InvalidOperationException("Provider boleto result does not match the reserved attempt.");

        // A canceled attempt is never reconciled, even if a late provider response reports payment.
        if (parent.Status == "Canceled" || parent.Status == "Succeeded")
            return true;

        boleto.ProviderPaymentId = charge.ProviderPaymentId;
        boleto.ProviderStatus = charge.ProviderStatus;
        boleto.IdentificationField = charge.IdentificationField ?? boleto.IdentificationField;
        boleto.Barcode = charge.Barcode ?? boleto.Barcode;
        boleto.BankSlipNumber = charge.BankSlipNumber ?? boleto.BankSlipNumber;
        boleto.BankSlipUrl = charge.BankSlipUrl ?? boleto.BankSlipUrl;
        boleto.InvoiceUrl = charge.InvoiceUrl ?? boleto.InvoiceUrl;

        if (charge.State is BoletoPaymentState.Confirmed or BoletoPaymentState.Received)
        {
            var receipt = await db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item =>
                item.TenantId == attempt.TenantId && item.ConnectionId == attempt.ConnectionId
                && item.ProviderTransactionId == charge.ProviderPaymentId, token);
            if (receipt is not null && (receipt.PaymentAttemptId != attempt.Id
                || receipt.ChargeId != attempt.ChargeId || receipt.Amount != charge.Amount))
                throw new InvalidOperationException("Provider boleto receipt conflicts with an existing payment.");
            if (receipt is null)
                db.PaymentTransactions.Add(new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = attempt.TenantId,
                    ChargeId = attempt.ChargeId,
                    PaymentAttemptId = attempt.Id,
                    ConnectionId = attempt.ConnectionId,
                    ProviderTransactionId = charge.ProviderPaymentId,
                    Amount = charge.Amount,
                    ReceivedAt = clock.GetUtcNow().UtcDateTime,
                    ProviderPayload = charge.ProviderPayloadJson
                });

            parent.Status = "Succeeded";
            parent.IssuanceBlocked = false;
            parent.RiskClosedAt = clock.GetUtcNow().UtcDateTime;
            parent.RiskClosureEvidence = "ProviderConfirmedBoletoPayment";
            parent.FailureCode = null;
            parent.FailureMessage = null;
            await FlushPaymentChangesAsync(token);
            if (!await HasSettlementJobAsync(attempt.TenantId, attempt.ChargeId, token))
                await (jobs ?? throw new InvalidOperationException("JobService is required to persist boleto payments."))
                    .CreateAsync(attempt.TenantId, new SettleChargeJob(attempt.ChargeId), token);
        }
        else if (charge.State == BoletoPaymentState.Pending)
        {
            parent.Status = "AwaitingPayment";
            parent.IssuanceBlocked = true;
            parent.ExpiresAt = null;
            parent.FailureCode = null;
            parent.FailureMessage = null;
        }
        else if (charge.State == BoletoPaymentState.Unknown)
        {
            parent.Status = "Unknown";
            parent.IssuanceBlocked = true;
            parent.FailureCode = "UnknownProviderStatus";
            parent.FailureMessage = "O provider retornou um status de boleto ainda não reconhecido.";
        }
        else
        {
            parent.Status = "Failed";
            parent.IssuanceBlocked = false;
            parent.RiskClosedAt = clock.GetUtcNow().UtcDateTime;
            parent.RiskClosureEvidence = "ProviderConfirmedBoletoUnavailable";
            parent.FailureCode = "BoletoUnavailable";
            parent.FailureMessage = "O boleto não está mais disponível para pagamento.";
        }

        PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
        return true;
    }, cancellationToken);
}
