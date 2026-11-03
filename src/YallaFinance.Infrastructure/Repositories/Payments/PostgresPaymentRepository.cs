using YallaFinance.Application.Common.Context;
using YallaFinance.Infrastructure.Database.Auditing;
using E = YallaFinance.Infrastructure.Database.Generated.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using YallaFinance.Application.Common.Errors;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.PaymentMethods;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Application.Services.Payments.Boleto.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Debts;
using YallaFinance.Domain.Payments;
using YallaFinance.Infrastructure.Utils.Database;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Transactions;
using YallaFinance.Infrastructure.Services.Payments;

namespace YallaFinance.Infrastructure.Repositories.Payments;

public sealed partial class PostgresPaymentRepository : IPaymentRepository
{
    private readonly YallaFinanceDbContext db;
    private readonly IUnitOfWork unitOfWork;
    private readonly IPaymentMethodRepository paymentMethods;
    private readonly TimeProvider clock;
    private readonly IPaymentPublicationSettings settings;
    private readonly JobService? jobs;
    private readonly CurrentExecutionActorContext actorContext;

    public PostgresPaymentRepository(
        YallaFinanceDbContext db,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        IPaymentMethodRepository paymentMethods,
        IPaymentPublicationSettings settings,
        CurrentExecutionActorContext actorContext,
        JobService? jobs = null)
    {
        this.db = db;
        this.actorContext = actorContext;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
        this.paymentMethods = paymentMethods;
        this.settings = settings;
        this.jobs = jobs;
    }

    public async Task<NotificationResult<PaymentRequestCreated>> PublishAsync(
        Guid tenant,
        PublishPaymentRequestData data,
        CancellationToken ct)
    {
        if (tenant == Guid.Empty || data.ChargeId == Guid.Empty
            || string.IsNullOrWhiteSpace(data.IdempotencyKey) || data.IdempotencyKey.Length > 200
            || data.PaymentMethodIds is not { Count: > 0 } || data.PaymentMethodIds.Contains(Guid.Empty)
            || data.PaymentMethodIds.Distinct().Count() != data.PaymentMethodIds.Count || data.Note?.Length > 2000)
            return CreatePaymentRequestFailureResult(ErrorCodes.Validation, "Solicitação inválida.");
        try
        {
            return await unitOfWork.ExecuteAsync(
                async token =>
                {
                    if (!await LockChargeForPaymentAsync(tenant, data.ChargeId, token))
                        return CreatePaymentRequestFailureResult(
                            ErrorCodes.NotFound,
                            "Cobrança não encontrada.");
                    var hash = PaymentCommandFingerprint.Hash(data);
                    var receipt = await db.PaymentCommandReceipts.AsNoTracking().SingleOrDefaultAsync(
                        r => r.TenantId == tenant
                            && r.ChargeId == data.ChargeId && r.Operation == "CreatePaymentRequest" && r.IdempotencyKey == data.IdempotencyKey,
                        token);
                    if (receipt is not null)
                    {
                        if (receipt.RequestHash != hash)
                            return CreatePaymentRequestFailureResult(
                                ErrorCodes.Conflict,
                                "Chave idempotente utilizada com outro conteúdo.");
                        var original = await db.PaymentRequests.AsNoTracking().SingleAsync(
                            r => r.TenantId == tenant &&
                                r.Id == receipt.PaymentRequestId,
                            token);
                        return NotificationResult<PaymentRequestCreated>.Success(CreatePaymentRequestCreatedResult(original, true));
                    }
                    var configuration = await settings.GetAsync(tenant, token);
                    var currentUtcTime = clock.GetUtcNow().UtcDateTime;
                    if (data.ExpiresAt.UtcDateTime <= currentUtcTime ||
                        data.ExpiresAt.UtcDateTime > currentUtcTime.AddDays(configuration.MaximumLinkDays))
                        return CreatePaymentRequestFailureResult(ErrorCodes.Validation, "Confira a validade do link.");
                    var charge = await GetChargeAsync(tenant, data.ChargeId, token);
                    if (charge is null)
                        return CreatePaymentRequestFailureResult(ErrorCodes.NotFound, "Cobrança não encontrada.");
                    var deadlineError = PaymentLinkValidity.Validate(data.ExpiresAt, charge.DueDate);
                    if (deadlineError is not null)
                        return CreatePaymentRequestFailureResult(ErrorCodes.Validation, deadlineError);
                    var activeRequests = await db.PaymentRequests.Where(r => r.TenantId == tenant && r.ChargeId == data.ChargeId
                        && r.Status == "Active").ToArrayAsync(token);
                    foreach (var paymentRequest in activeRequests)
                    {
                        if (paymentRequest.ExpiresAt <= currentUtcTime)
                        {
                            paymentRequest.Status = "Expired";
                            paymentRequest.ExpiredAt = currentUtcTime;
                        }
                        else
                        {
                            paymentRequest.Status = "Canceled";
                            paymentRequest.CanceledAt = currentUtcTime;
                            paymentRequest.CanceledBy = actorContext.RequireActorId().ToString();
                        }
                    }
                    if (activeRequests.Length > 0)
                        await db.SaveChangesAsync(token);
                    // Persist the replacement inside this transaction so the snapshot sees the new
                    // active request set. If an old attempt still carries financial risk, eligibility
                    // rejects the new request and the transaction rolls these cancellations back.
                    if (activeRequests.Length > 0)
                        charge = await GetChargeAsync(tenant, data.ChargeId, token);
                    if (charge is null)
                        return CreatePaymentRequestFailureResult(ErrorCodes.NotFound, "Cobrança não encontrada.");
                    if (!charge.Eligible)
                        return CreatePaymentRequestFailureResult(
                            ErrorCodes.Conflict,
                            "Cobrança indisponível: " + charge.BlockReason);
                    var available = await AvailableMethodsAsync(tenant, token);
                    if (data.PaymentMethodIds.Any(id => !available.Any(m => m.Id == id)))
                        return CreatePaymentRequestFailureResult(ErrorCodes.BusinessRule, "Método de pagamento indisponível.");
                    var tokenHash = Convert.ToHexStringLower(SHA256.HashData(RandomNumberGenerator.GetBytes(32)));
                    // Previous Active rows are already closed inside this transaction, satisfying the
                    // immediate unique partial index. A failed commit still rolls them back together.
                    var request = new E.PaymentRequest
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant,
                        ChargeId = data.ChargeId,
                        Status = "Active",
                        TokenHash = tokenHash,
                        ExpiresAt = data.ExpiresAt.UtcDateTime,
                        Note = data.Note,
                        CreatedAt = currentUtcTime,
                        UpdatedAt = null
                    };
                    db.PaymentRequests.Add(request);
                    db.PaymentRequestMethods.AddRange(data.PaymentMethodIds.Select(id => new E.PaymentRequestMethod
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant,
                        PaymentRequestId = request.Id,
                        PaymentMethodId = id,
                        CreatedAt = currentUtcTime
                    }));
                    db.PaymentCommandReceipts.Add(new E.PaymentCommandReceipt
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant,
                        ChargeId = data.ChargeId,
                        PaymentRequestId = request.Id,
                        Operation = "CreatePaymentRequest",
                        IdempotencyKey = data.IdempotencyKey,
                        RequestHash = hash,
                        CreatedAt = currentUtcTime
                    });
                    PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                    return NotificationResult<PaymentRequestCreated>.Success(CreatePaymentRequestCreatedResult(request));
                },
                ct);
        }
        catch (DBConcurrencyException)
        {
            return CreatePaymentRequestFailureResult(
                ErrorCodes.Conflict,
                "Cobrança ou solicitação utilizada por outra operação.");
        }
    }

    public async Task<NotificationResult<PaymentRequestCreated>> GetAsync(
        Guid tenant,
        Guid id,
        CancellationToken ct)
    {
        var paymentRequest = await db.PaymentRequests.AsNoTracking().SingleOrDefaultAsync(
            r => r.TenantId == tenant &&
                r.Id == id,
            ct);
        return paymentRequest is null ? CreatePaymentRequestFailureResult(
            ErrorCodes.NotFound,
            "Solicitação não encontrada.") : NotificationResult<PaymentRequestCreated>.Success(CreatePaymentRequestCreatedResult(paymentRequest));
    }

    public async Task<NotificationResult<PaymentLinkResult>> RecoverLinkAsync(
        Guid tenant,
        Guid id,
        CancellationToken ct)
    {
        var paymentRequest = await db.PaymentRequests.AsNoTracking().SingleOrDefaultAsync(
            r => r.TenantId == tenant &&
                r.Id == id,
            ct);
        if (paymentRequest is null)
            return NotificationResult<PaymentLinkResult>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Solicitação não encontrada."));
        var configuration = await settings.GetAsync(tenant, ct);
        return NotificationResult<PaymentLinkResult>.Success(PaymentLinkBuilder.Build(paymentRequest.Id, paymentRequest.TokenHash,
            configuration.PublicOrigin));
    }

    public async Task<NotificationResult<PublicPaymentCheckout>> GetPublicCheckoutAsync(
        string token,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token) ||
            token.Length != 64 ||
            token.Any(character => !Uri.IsHexDigit(character)))
            return CreatePublicPaymentNotFoundResult();

        var paymentRequest = await db.PaymentRequests.AsNoTracking().AsSplitQuery()
            .Include(request => request.Charge).ThenInclude(charge => charge.ChargeItems)
            .ThenInclude(item => item.Debt).ThenInclude(debt => debt.Contract)
            .Include(request => request.PaymentRequestMethods).ThenInclude(method => method.PaymentMethod)
            .SingleOrDefaultAsync(request => request.TokenHash == token.ToLowerInvariant(), ct);
        if (paymentRequest is null)
            return CreatePublicPaymentNotFoundResult();
        var publicReceipts = await db.PaymentTransactions.AsNoTracking().Where(transaction =>
            transaction.TenantId == paymentRequest.TenantId &&
            transaction.ChargeId == paymentRequest.ChargeId)
            .OrderBy(transaction => transaction.ReceivedAt).ThenBy(transaction => transaction.Id)
            .Select(transaction => new PublicPaymentReceiptSummary(
                transaction.Amount,
                transaction.ReceivedAt,
                db.PaymentMethods.Where(method => method.TenantId == paymentRequest.TenantId &&
                    method.Id == transaction.PaymentAttempt.PaymentMethodId)
                    .Select(method => method.Name).FirstOrDefault() ?? "Indisponível"))
            .ToArrayAsync(ct);
        var settledAt = await db.PaymentSettlements.AsNoTracking().Where(settlement =>
            settlement.TenantId == paymentRequest.TenantId &&
            settlement.ChargeId == paymentRequest.ChargeId)
            .MaxAsync(settlement => (DateTime?)settlement.SettledAt, ct);
        var hasReceivedPayment = publicReceipts.Length > 0;
        var receivedAmount = publicReceipts.Sum(receipt => receipt.Amount);
        var receivedAt = publicReceipts.LastOrDefault()?.ReceivedAt;
        var status = paymentRequest.Status == "Active" &&
            paymentRequest.ExpiresAt <= clock.GetUtcNow()
                .UtcDateTime ? "Expired" : paymentRequest.Status;
        var contractNumber = paymentRequest.Charge.ChargeItems.OrderBy(item => item.Ordinal)
            .Select(item => item.Debt.Contract.Number).FirstOrDefault() ?? "";
        var methods = paymentRequest.PaymentRequestMethods.OrderBy(method => method.PaymentMethod.Name)
            .Where(method => method.PaymentMethod.TenantId == paymentRequest.TenantId &&
                method.PaymentMethod.Active)
            .Select(method => new PublicPaymentMethodOption(method.PaymentMethod.Code,
                method.PaymentMethod.Name, method.PaymentMethod.InstrumentType)).ToArray();
        var attempt = await db.PaymentAttempts.AsNoTracking()
            .Where(item => item.TenantId == paymentRequest.TenantId &&
                item.PaymentRequestId == paymentRequest.Id)
            .OrderByDescending(item => item.IssuanceBlocked)
            .ThenByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id).FirstOrDefaultAsync(ct);
        var attemptPix = attempt is null ? null : await db.PaymentAttemptPixes.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TenantId == paymentRequest.TenantId && item.PaymentAttemptId == attempt.Id,
                ct);
        var attemptBoleto = attempt is null ? null : await db.PaymentAttemptBoletos.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TenantId == paymentRequest.TenantId && item.PaymentAttemptId == attempt.Id,
                ct);
        var attemptMethodCode = attempt is null ? null : paymentRequest.PaymentRequestMethods
            .Where(method => method.PaymentMethodId == attempt.PaymentMethodId)
            .Select(method => method.PaymentMethod.Code)
            .FirstOrDefault();
        var pixExpired = attempt?.Status == "AwaitingPayment" &&
            attempt.ExpiresAt <= clock.GetUtcNow()
                .UtcDateTime;
        var attemptInstrument = attempt is null ? null : await ReadAttemptInstrumentAsync(attempt, ct);
        PublicPaymentAttemptResult? publicAttempt = attempt is null ? null : new PublicPaymentAttemptResult(
            pixExpired ? "Expired" : attempt.Status,
            attempt.Amount,
            attempt.ExpiresAt,
            pixExpired ? null : attemptPix?.Brcode,
            pixExpired ? null : attemptPix?.Location,
            (attempt.Status == "Failed" && !attempt.IssuanceBlocked) || pixExpired,
            pixExpired ? "Este Pix expirou. Gere um novo código para continuar." : attempt.Status switch
            {
                "Processing" or "Pending" => "Estamos preparando seu pagamento.",
                "Unknown" => attemptInstrument switch
                {
                    YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard => "Estamos verificando seu pagamento com cartão. Não pague novamente.",
                    YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto => "Estamos verificando a situação do boleto.",
                    _ => "Não foi possível confirmar a emissão. Atualize a página antes de tentar novamente."
                },
                "Failed" => attemptInstrument switch
                {
                    YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard => "Não foi possível concluir o pagamento com cartão. Confira os dados ou use outro cartão.",
                    YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto => "Não foi possível gerar o boleto. Você pode tentar novamente.",
                    _ => "Não foi possível gerar o Pix. Você pode tentar novamente."
                },
                _ => null
            },
            attemptMethodCode)
        {
            InstrumentType = attemptInstrument?.ToString(),
            AttemptReference = attempt.PublicId,
            Boleto = pixExpired || attemptBoleto is null ? null : new PublicBoletoDetails(
                attemptBoleto.IdentificationField,
                attemptBoleto.Barcode,
                attemptBoleto.BankSlipNumber,
                attemptBoleto.BankSlipUrl,
                attemptBoleto.InvoiceUrl)
        };
        var customer = methods.Any(method => method.InstrumentType == "CreditCard")
            ? await ReadCardCustomerAsync(paymentRequest.TenantId, paymentRequest.ChargeId, ct) : null;
        return NotificationResult<PublicPaymentCheckout>.Success(new(
            status,
            paymentRequest.ExpiresAt,
            paymentRequest.Charge.Amount,
            paymentRequest.Charge.DueDate,
            contractNumber,
            methods,
            hasReceivedPayment ? null : publicAttempt,
            hasReceivedPayment,
            receivedAmount,
            receivedAt,
            settledAt is not null,
            settledAt,
            publicReceipts) { CardRequiredFields = customer is null ? null : PublicCardPaymentInput.MissingCustomerFields(customer) });
    }

    public Task<NotificationResult<PaymentRequestCreated>> CancelAsync(
        Guid tenant,
        Guid id,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(
            async token =>
            {
                var chargeId = await db.PaymentRequests.AsNoTracking().Where(r => r.TenantId == tenant &&
                    r.Id == id)
                    .Select(r => (Guid?)r.ChargeId)
                    .SingleOrDefaultAsync(token);
                if (chargeId is null || !await LockChargeForPaymentAsync(tenant, chargeId.Value, token))
                    return CreatePaymentRequestFailureResult(
                        ErrorCodes.NotFound,
                        "Solicitação não encontrada.");
                var paymentRequest = await db.PaymentRequests.SingleAsync(
                    r => r.TenantId == tenant && r.Id == id,
                    token);
                if (paymentRequest.Status == "Completed")
                    return CreatePaymentRequestFailureResult(
                        ErrorCodes.Conflict,
                        "Solicitação concluída não pode ser cancelada.");
                if (paymentRequest.Status != "Active")
                    return NotificationResult<PaymentRequestCreated>.Success(CreatePaymentRequestCreatedResult(paymentRequest));
                var currentUtcTime = clock.GetUtcNow().UtcDateTime;
                if (paymentRequest.ExpiresAt <= currentUtcTime)
                {
                    paymentRequest.Status = "Expired";
                    paymentRequest.ExpiredAt = currentUtcTime;
                }
                else
                {
                    paymentRequest.Status = "Canceled";
                    paymentRequest.CanceledAt = currentUtcTime;
                    paymentRequest.CanceledBy = actorContext.RequireActorId().ToString();
                }
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                return NotificationResult<PaymentRequestCreated>.Success(CreatePaymentRequestCreatedResult(paymentRequest));
            },
            ct);

    private async Task<bool> LockChargeForPaymentAsync(Guid tenantId, Guid chargeId, CancellationToken cancellationToken) =>
        (await db.Charges.FromSqlInterpolated($"SELECT * FROM finance.charge WHERE tenant_id = {tenantId} AND id = {chargeId} FOR UPDATE")
            .ToArrayAsync(cancellationToken)).Length != 0;

    private PaymentRequestCreated CreatePaymentRequestCreatedResult(E.PaymentRequest paymentRequest, bool isReplay = false) => new(
        paymentRequest.Id,
        paymentRequest.ChargeId,
        paymentRequest.Status == "Active" && paymentRequest.ExpiresAt <= clock.GetUtcNow().UtcDateTime
            ? "Expired"
            : paymentRequest.Status,
        paymentRequest.ExpiresAt,
        paymentRequest.CreatedAt,
        isReplay);

    private static NotificationResult<PaymentRequestCreated> CreatePaymentRequestFailureResult(string code, string message) =>
        NotificationResult<PaymentRequestCreated>.Failure(new NotificationError(code, message));

    private static NotificationResult<PublicPaymentCheckout> CreatePublicPaymentNotFoundResult() =>
        NotificationResult<PublicPaymentCheckout>.Failure(new NotificationError(
            ErrorCodes.NotFound,
            "Solicitação não encontrada."));
    public IQueryable<E.Charge> ChargesForPayment(Guid tenantId) => db.Charges.AsNoTracking()
        .Where(charge => charge.TenantId == tenantId);

    public async Task<PaymentChargeListResult> ListAsync(
        Guid tenantId,
        PaymentChargeListFilter filter,
        CancellationToken ct)
    {
        var query = FilterByReference(ChargesForPayment(tenantId), filter.Reference);
        if (filter.StartDate is { } startDate)
        {
            var utc = DayStart(startDate);
            query = query.Where(charge => charge.CreatedAt >= utc);
        }
        if (filter.EndDate is { } endDate)
        {
            var utc = DayStart(endDate.AddDays(1));
            query = query.Where(charge => charge.CreatedAt < utc);
        }
        var statusBaseQuery = query;
        var statusNow = clock.GetUtcNow().UtcDateTime;
        var awaitingPaymentCount = await FilterByPaymentStatus(
            statusBaseQuery,
            tenantId,
            "awaitingPayment",
            statusNow)
            .CountAsync(ct);
        var expiredPaymentCount = await FilterByPaymentStatus(statusBaseQuery, tenantId, "expired", statusNow)
            .CountAsync(ct);
        var availablePaymentCount = await FilterByPaymentStatus(
            statusBaseQuery,
            tenantId,
            "available",
            statusNow)
            .CountAsync(ct);
        if (filter.PaymentStatus is { } paymentStatus)
            query = FilterByPaymentStatus(query, tenantId, paymentStatus, statusNow);
        var count = await query.CountAsync(ct);
        var total = await query.SumAsync(charge => (decimal?)charge.Amount, ct) ?? 0;
        var charges = await IncludeChargeItemDetails(Sort(query, filter.SortBy, filter.SortDirection))
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArrayAsync(ct);
        var chargeIds = charges.Select(charge => charge.Id).ToArray();
        var chargeIdsWithBlockedIssuance = await db.PaymentAttempts.AsNoTracking().Where(paymentAttempt => paymentAttempt.TenantId == tenantId &&
            chargeIds.Contains(paymentAttempt.ChargeId) &&
            paymentAttempt.IssuanceBlocked)
            .Select(paymentAttempt => paymentAttempt.ChargeId).Distinct().ToArrayAsync(ct);
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        var activeRequests = await db.PaymentRequests.AsNoTracking().Where(paymentRequest => paymentRequest.TenantId == tenantId && chargeIds.Contains(paymentRequest.ChargeId)
            && paymentRequest.Status == "Active" && paymentRequest.ExpiresAt > currentUtcTime)
            .Select(paymentRequest => new
            {
                paymentRequest.ChargeId,
                Request = new ActivePaymentRequestSummary(
                    paymentRequest.Id,
                    paymentRequest.Status,
                    paymentRequest.CreatedAt,
                    paymentRequest.ExpiresAt)
            })
            .ToDictionaryAsync(r => r.ChargeId, r => r.Request, ct);
        var activity = await ReadPaymentActivityAsync(tenantId, chargeIds, ct);
        var settlements = await db.PaymentSettlements.AsNoTracking().Where(paymentSettlement => paymentSettlement.TenantId == tenantId &&
            chargeIds.Contains(paymentSettlement.ChargeId))
            .GroupBy(paymentSettlement => paymentSettlement.ChargeId).Select(group => new { ChargeId = group.Key, SettledAt = group.Max(paymentSettlement => paymentSettlement.SettledAt) })
            .ToDictionaryAsync(row => row.ChargeId, row => (DateTime?)row.SettledAt, ct);
        var receiptRows = await db.PaymentTransactions.AsNoTracking().Where(paymentTransaction => paymentTransaction.TenantId == tenantId &&
            chargeIds.Contains(paymentTransaction.ChargeId))
            .OrderBy(paymentTransaction => paymentTransaction.ReceivedAt).ThenBy(paymentTransaction => paymentTransaction.Id)
            .Select(paymentTransaction => new
            {
                paymentTransaction.ChargeId,
                Receipt = new PaymentReceiptSummary(
                    paymentTransaction.Id,
                    paymentTransaction.EndToEndId,
                    paymentTransaction.Amount,
                    paymentTransaction.ReceivedAt,
                    db.PaymentMethods.Where(method => method.TenantId == tenantId &&
                        method.Id == paymentTransaction.PaymentAttempt.PaymentMethodId)
                        .Select(method => method.Name).FirstOrDefault() ?? "Indisponível",
                    paymentTransaction.PaymentAttempt.Provider) { ProviderTransactionId = paymentTransaction.ProviderTransactionId }
            })
            .ToArrayAsync(ct);
        var receiptsByCharge = receiptRows.GroupBy(row => row.ChargeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PaymentReceiptSummary>)group.Select(row => row.Receipt)
                    .ToArray());
        return new(
            charges.Select(charge => CreatePaymentChargeSnapshot(
                charge,
                chargeIdsWithBlockedIssuance.Contains(charge.Id),
                activeRequests.GetValueOrDefault(charge.Id),
                activity.GetValueOrDefault(charge.Id),
                receiptsByCharge.GetValueOrDefault(charge.Id),
                settlements.GetValueOrDefault(charge.Id))).ToArray(),
            filter.Page,
            filter.PageSize,
            count,
            total,
            awaitingPaymentCount,
            expiredPaymentCount,
            availablePaymentCount);
    }

    internal IQueryable<E.Charge> FilterByReference(IQueryable<E.Charge> query, string? reference)
    {
        if (reference is null)
            return query;
        if (!ChargeReference.TryParse(reference, out var code, out var number))
            throw new ArgumentException("Invalid charge reference.", nameof(reference));
        return query.Where(charge => charge.Number == number && charge.Tenant.Code == code);
    }

    internal IQueryable<E.Charge> FilterByPaymentStatus(
        IQueryable<E.Charge> query,
        Guid tenantId,
        string paymentStatus,
        DateTime now)
    {
        // Financial evidence takes precedence over stale link/attempt state.
        // Reuse this restriction for both the status counters and the filtered list.
        query = query.Where(charge =>
            !db.PaymentTransactions.Any(receipt => receipt.TenantId == tenantId
                && receipt.ChargeId == charge.Id)
            && !db.PaymentSettlements.Any(settlement => settlement.TenantId == tenantId
                && settlement.ChargeId == charge.Id));

        if (paymentStatus == "available")
            return query.Where(PaymentChargeEligibilityQuery.IsAvailable(db, now));
        if (paymentStatus != "awaitingPayment" && paymentStatus != "expired")
            return query.Where(charge => false);

        var chargePaymentStatuses = query.Select(charge => new
        {
            Charge = charge,
            LatestBlockedAttempt = db.PaymentAttempts.Where(paymentAttempt => paymentAttempt.TenantId == tenantId &&
                paymentAttempt.ChargeId == charge.Id &&
                paymentAttempt.IssuanceBlocked)
                .OrderByDescending(paymentAttempt => paymentAttempt.UpdatedAt ?? paymentAttempt.CreatedAt).ThenByDescending(paymentAttempt => paymentAttempt.Id).Select(paymentAttempt => paymentAttempt.Status).FirstOrDefault(),
            LatestAttempt = db.PaymentAttempts.Where(paymentAttempt => paymentAttempt.TenantId == tenantId &&
                paymentAttempt.ChargeId == charge.Id)
                .OrderByDescending(paymentAttempt => paymentAttempt.UpdatedAt ?? paymentAttempt.CreatedAt).ThenByDescending(paymentAttempt => paymentAttempt.Id).Select(paymentAttempt => paymentAttempt.Status).FirstOrDefault(),
            LatestRequestStatus = db.PaymentRequests.Where(paymentRequest => paymentRequest.TenantId == tenantId &&
                paymentRequest.ChargeId == charge.Id)
                .OrderByDescending(paymentRequest => paymentRequest.CreatedAt).ThenByDescending(paymentRequest => paymentRequest.Id).Select(paymentRequest => paymentRequest.Status).FirstOrDefault(),
            LatestRequestExpiresAt = db.PaymentRequests.Where(paymentRequest => paymentRequest.TenantId == tenantId &&
                paymentRequest.ChargeId == charge.Id)
                .OrderByDescending(paymentRequest => paymentRequest.CreatedAt).ThenByDescending(paymentRequest => paymentRequest.Id).Select(paymentRequest => paymentRequest.ExpiresAt).FirstOrDefault()
        });

        chargePaymentStatuses = paymentStatus switch
        {
            // An open financial instrument always takes precedence. Otherwise, when a request
            // is active, its lifecycle determines the list status; attempts from older requests
            // must not make the current request appear expired.
            "awaitingPayment" => chargePaymentStatuses.Where(x => x.LatestBlockedAttempt != null
                || (x.LatestRequestStatus == "Active" && x.LatestRequestExpiresAt > now)),
            "expired" => chargePaymentStatuses.Where(x => x.LatestBlockedAttempt == null
                && (x.LatestRequestStatus == "Active"
                    ? x.LatestRequestExpiresAt <= now
                    : x.LatestRequestStatus == "Expired" || x.LatestAttempt == "Expired")),
            _ => chargePaymentStatuses.Where(_ => false)
        };

        return chargePaymentStatuses.Select(x => x.Charge);
    }

    public async Task<PaymentChargeSnapshot?> GetChargeAsync(Guid tenantId, Guid chargeId, CancellationToken ct)
    {
        var charge = await IncludeChargeItemDetails(db.Charges.AsNoTracking()).SingleOrDefaultAsync(
            c => c.TenantId == tenantId &&
                c.Id == chargeId,
            ct);
        if (charge is null)
            return null;
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        var activeRequest = await db.PaymentRequests.AsNoTracking().Where(paymentRequest => paymentRequest.TenantId == tenantId && paymentRequest.ChargeId == chargeId
            && paymentRequest.Status == "Active" && paymentRequest.ExpiresAt > currentUtcTime)
            .Select(paymentRequest => new ActivePaymentRequestSummary(
                paymentRequest.Id,
                paymentRequest.Status,
                paymentRequest.CreatedAt,
                paymentRequest.ExpiresAt)).SingleOrDefaultAsync(ct);
        var activity = (await ReadPaymentActivityAsync(tenantId, [chargeId], ct)).GetValueOrDefault(chargeId) ?? new(
            null,
            null,
            null);
        var receipts = await db.PaymentTransactions.AsNoTracking().Where(paymentTransaction => paymentTransaction.TenantId == tenantId &&
            paymentTransaction.ChargeId == chargeId)
            .OrderBy(paymentTransaction => paymentTransaction.ReceivedAt).ThenBy(paymentTransaction => paymentTransaction.Id)
            .Select(paymentTransaction => new PaymentReceiptSummary(
                paymentTransaction.Id,
                paymentTransaction.EndToEndId,
                paymentTransaction.Amount,
                paymentTransaction.ReceivedAt,
                db.PaymentMethods.Where(method => method.TenantId == tenantId &&
                    method.Id == paymentTransaction.PaymentAttempt.PaymentMethodId)
                    .Select(method => method.Name).FirstOrDefault() ?? "Indisponível",
                paymentTransaction.PaymentAttempt.Provider) { ProviderTransactionId = paymentTransaction.ProviderTransactionId })
            .ToArrayAsync(ct);
        var settledAt = await db.PaymentSettlements.AsNoTracking().Where(paymentSettlement => paymentSettlement.TenantId == tenantId &&
            paymentSettlement.ChargeId == chargeId)
            .MaxAsync(paymentSettlement => (DateTime?)paymentSettlement.SettledAt, ct);
        return CreatePaymentChargeSnapshot(charge,
            await db.PaymentAttempts.AnyAsync(
                paymentAttempt => paymentAttempt.TenantId == tenantId &&
                    paymentAttempt.ChargeId == chargeId &&
                    paymentAttempt.IssuanceBlocked,
                ct),
            activeRequest, activity, receipts, settledAt);
    }

    public async Task<PixChargeCreationContext?> GetPixChargeCreationContextAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        var charge = await db.Charges.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.Id == chargeId)
            .Select(row => new { row.DueDate })
            .SingleOrDefaultAsync(cancellationToken);
        if (charge is null)
            return null;

        var customerIds = await db.ChargeItems.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.ChargeId == chargeId)
            .Select(row => row.Debt.Contract.CustomerId)
            .Distinct()
            .Take(2)
            .ToArrayAsync(cancellationToken);
        if (customerIds.Length != 1)
            return new(charge.DueDate, null);

        var customer = await db.Customers.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.Id == customerIds[0])
            .Select(row => new { row.Id, row.Name, row.Document })
            .SingleOrDefaultAsync(cancellationToken);
        var payer = customer is null ? null : new PixChargePayer(customer.Document, customer.Name, customer.Id);
        return new(charge.DueDate, payer);
    }

    public async Task<PaymentChargeCreationContext?> GetPaymentChargeCreationContextAsync(
        Guid tenantId,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        var pixContext = await GetPixChargeCreationContextAsync(tenantId, chargeId, cancellationToken);
        return pixContext is null
            ? null
            : new PaymentChargeCreationContext(pixContext.DueDate, pixContext.Payer is null
                ? null
                : new PaymentChargePayer(pixContext.Payer.TaxId, pixContext.Payer.Name, pixContext.Payer.CustomerId));
    }

    public Task<IReadOnlyList<PaymentMethodOption>> AvailableMethodsAsync(Guid tenantId, CancellationToken ct) => ReadPaymentMethodsAsync(
        tenantId,
        ct);

    public IOrderedQueryable<E.Charge> Sort(IQueryable<E.Charge> query, string? key, string direction)
    {
        if (direction is not ("asc" or "desc"))
            throw new ArgumentException("Invalid sort direction.");
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        System.Linq.Expressions.Expression<Func<E.Charge, object>> sortExpression = key switch
        {
            null or "created" => c => c.CreatedAt,
            "charge" => c => c.Number,
            "customer" => c => c.ChargeItems.OrderBy(chargeItem => chargeItem.Id).Select(chargeItem => chargeItem.Debt.Contract.Customer.Name)
                .FirstOrDefault()!,
            "contract" => c => c.ChargeItems.Select(chargeItem => chargeItem.Debt.ContractId).Distinct().Count() == 1
                ? c.ChargeItems.Select(chargeItem => chargeItem.Debt.Contract.Number).FirstOrDefault()! : "Múltiplos contratos",
            "due" => c => c.DueDate,
            "amount" => c => c.Amount,
            "eligibility" => PaymentChargeEligibilityQuery.AsSortValue(db, currentUtcTime),
            _ => throw new ArgumentException("Invalid sort key.")
        };
        return (direction == "asc" ? query.OrderBy(sortExpression) : query.OrderByDescending(sortExpression)).ThenBy(c => c.Id);
    }

    private async Task<IReadOnlyList<PaymentMethodOption>> ReadPaymentMethodsAsync(
        Guid tenantId,
        CancellationToken ct) =>
        (await paymentMethods.ListAsync(tenantId.ToString(), ct))
            .Select(method => new PaymentMethodOption(Guid.Parse(method.Id), method.Name, method.Code, method.InstrumentType,
                method.InternalDescription)).ToArray();

    private async Task<Dictionary<Guid, PaymentActivitySummary>> ReadPaymentActivityAsync(
        Guid tenantId,
        Guid[] chargeIds,
        CancellationToken ct)
    {
        var requests = await db.PaymentRequests.AsNoTracking().Where(paymentRequest => paymentRequest.TenantId == tenantId &&
            chargeIds.Contains(paymentRequest.ChargeId))
            .Select(paymentRequest => new
            {
                paymentRequest.ChargeId,
                Activity = new PaymentRequestActivitySummary(
                    paymentRequest.Id,
                    paymentRequest.Status,
                    paymentRequest.CreatedAt,
                    paymentRequest.ExpiresAt,
                    paymentRequest.ExpiredAt,
                    paymentRequest.CanceledAt,
                    paymentRequest.CompletedAt,
                    paymentRequest.CompletionReason)
            }).ToArrayAsync(ct);
        var attempts = await db.PaymentAttempts.AsNoTracking().Where(paymentAttempt => paymentAttempt.TenantId == tenantId &&
            chargeIds.Contains(paymentAttempt.ChargeId))
            .Select(paymentAttempt => new
            {
                paymentAttempt.ChargeId,
                Activity = new PaymentAttemptActivitySummary(
                    paymentAttempt.Status,
                    paymentAttempt.Provider,
                    db.PaymentMethods.Where(paymentMethod => paymentMethod.TenantId == tenantId &&
                        paymentMethod.Id == paymentAttempt.PaymentMethodId)
                        .Select(paymentMethod => paymentMethod.Name)
                        .FirstOrDefault() ?? "Indisponível",
                    paymentAttempt.CreatedAt,
                    paymentAttempt.UpdatedAt,
                    paymentAttempt.ExpiresAt,
                    paymentAttempt.IssuanceBlocked,
                    paymentAttempt.FailureCode,
                    paymentAttempt.FailureMessage)
                {
                    BoletoPdfUrl = db.PaymentAttemptBoletos.Where(boleto => boleto.TenantId == tenantId
                            && boleto.PaymentAttemptId == paymentAttempt.Id)
                        .Select(boleto => boleto.BankSlipUrl)
                        .FirstOrDefault()
                }
            }).ToArrayAsync(ct);
        var requestIds = requests.Select(r => r.Activity.Id).ToArray();
        var methods = await db.PaymentRequestMethods.AsNoTracking().Where(paymentRequestMethod => paymentRequestMethod.TenantId == tenantId &&
            requestIds.Contains(paymentRequestMethod.PaymentRequestId))
            .Select(paymentRequestMethod => new { paymentRequestMethod.PaymentRequestId, paymentRequestMethod.PaymentMethod.Name }).ToArrayAsync(ct);
        return chargeIds.Distinct().ToDictionary(
            id => id,
            id => new PaymentActivitySummary(
                requests.Where(r => r.ChargeId == id).OrderByDescending(r => r.Activity.CreatedAt)
                    .Select(r => r.Activity with
                    {
                        AuthorizedPaymentMethods = methods.Where(m => m.PaymentRequestId == r.Activity.Id)
                            .Select(m => m.Name).OrderBy(name => name).ToArray()
                    }).FirstOrDefault(),
                attempts.Where(a => a.ChargeId == id).OrderByDescending(a => a.Activity.UpdatedAt ?? a.Activity.CreatedAt).Select(a => a.Activity)
                    .FirstOrDefault(),
                attempts.Where(a => a.ChargeId == id && a.Activity.IssuanceBlocked).OrderByDescending(a => a.Activity.UpdatedAt ?? a.Activity.CreatedAt)
                    .Select(a => a.Activity).FirstOrDefault()));
    }

    private static IQueryable<E.Charge> IncludeChargeItemDetails(IQueryable<E.Charge> query) => query
        .Include(charge => charge.Tenant)
        .Include(charge => charge.ChargeItems).ThenInclude(chargeItem => chargeItem.Debt).ThenInclude(debt => debt.Contract).ThenInclude(contract => contract.Customer)
        .Include(charge => charge.ChargeItems).ThenInclude(chargeItem => chargeItem.Debt).ThenInclude(debt => debt.Category);

    internal static PaymentChargeSnapshot CreatePaymentChargeSnapshot(
        E.Charge row,
        bool risk,
        ActivePaymentRequestSummary? activeRequest,
        PaymentActivitySummary? activity,
        IReadOnlyList<PaymentReceiptSummary>? receipts,
        DateTime? settledAt)
    {
        var items = row.ChargeItems.ToArray();
        var contractIds = items.Select(chargeItem => chargeItem.Debt.ContractId).Distinct().ToArray();
        var areChargeItemsValid = items.All(chargeItem => chargeItem.TenantId == row.TenantId && chargeItem.Debt.TenantId == row.TenantId
            && chargeItem.Amount > 0 && chargeItem.Amount == chargeItem.Debt.Amount && chargeItem.Debt.Status is "Open" or "InCollection"
            && chargeItem.Debt.Contract.TenantId == row.TenantId && chargeItem.Debt.Contract.Active
            && chargeItem.Debt.Contract.Customer.TenantId == row.TenantId && chargeItem.Debt.Contract.Customer.Active
            && chargeItem.Debt.Category.TenantId == row.TenantId && chargeItem.Debt.Category.Active);
        var paymentBlockReason = new PaymentEligibility(
            row.Amount,
            items.Sum(chargeItem => chargeItem.Amount),
            contractIds.Length,
            items.Length,
            areChargeItemsValid,
            risk,
            activeRequest is not null).BlockReason();
        return new(
            row.Id,
            items.FirstOrDefault()?.Debt.Contract.Customer.Name ?? "—",
            contractIds.Length == 1 ? items[0].Debt.Contract.Number : "Múltiplos contratos",
            row.Amount,
            row.CreatedAt,
            row.DueDate,
            paymentBlockReason is null,
            paymentBlockReason,
            activeRequest,
            activity,
            receipts?.Sum(receipt => receipt.Amount) ?? 0m,
            settledAt is not null,
            receipts?.LastOrDefault()?.ReceivedAt,
            settledAt,
            receipts ?? [],
            ChargeReference.Format(row.Tenant.Code, row.Number));
    }

    public static DateTime DayStart(DateOnly date)
    {
        var saoPauloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var localDayStart = date.ToDateTime(TimeOnly.MinValue);
        // On historical DST transitions, the first valid instant can be after midnight.
        while (saoPauloTimeZone.IsInvalidTime(localDayStart))
            localDayStart = localDayStart.AddMinutes(1);
        if (saoPauloTimeZone.IsAmbiguousTime(localDayStart))
            return new DateTimeOffset(
                localDayStart,
                saoPauloTimeZone.GetAmbiguousTimeOffsets(localDayStart)
                    .Max()).UtcDateTime;
        return TimeZoneInfo.ConvertTimeToUtc(localDayStart, saoPauloTimeZone);
    }
    public async Task<NotificationResult<PaymentReceiptPersistenceResult>> PersistReceivedReceiptsAsync(
        Guid tenantId,
        Guid connectionId,
        string provider,
        IReadOnlyList<PixReceivedTransaction> receipts,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || connectionId == Guid.Empty || string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Receipt tenant, bank connection and provider are required.");

        var receiptsWithTransactionId = receipts.DistinctBy(receipt => receipt.EndToEndId, StringComparer.Ordinal)
            .Where(receipt => !string.IsNullOrWhiteSpace(receipt.TxId))
            .ToArray();
        if (receiptsWithTransactionId.Length == 0)
            return NotificationResult<PaymentReceiptPersistenceResult>.Success(new(0, 0, 0, 0, 0));

        var matchedReceipts = await MatchPaymentReceiptsAsync(
            tenantId,
            connectionId,
            provider,
            receiptsWithTransactionId,
            cancellationToken);
        if (matchedReceipts.Any(matchedReceipt => matchedReceipt.TenantId != tenantId
            || matchedReceipt.BankConnectionId != connectionId
            || matchedReceipt.ChargeId == Guid.Empty
            || matchedReceipt.PaymentAttemptId == Guid.Empty
            || !receiptsWithTransactionId.Contains(matchedReceipt.Receipt)))
            throw new InvalidOperationException("Receipt matching returned an inconsistent identity.");

        var newlyStoredReceiptCount = 0;
        foreach (var chargeReceiptGroup in matchedReceipts.GroupBy(matchedReceipt => matchedReceipt.ChargeId)
            .OrderBy(group => group.Key))
        {
            var groupPersistenceResult = await unitOfWork.ExecuteAsync(
                async transactionToken =>
                {
                    await LockChargeForReceiptAsync(tenantId, chargeReceiptGroup.Key, transactionToken);
                    var newlyInsertedReceiptCount = 0;
                    foreach (var matchedReceipt in chargeReceiptGroup)
                        if (await PersistMatchedReceiptAsync(matchedReceipt, transactionToken))
                            newlyInsertedReceiptCount++;

                    await FlushPaymentChangesAsync(transactionToken);
                    // Preserve existing jobs, including failed ones; retries remain manual.
                    if (!await HasSettlementJobAsync(tenantId, chargeReceiptGroup.Key, transactionToken))
                        await (jobs ?? throw new InvalidOperationException("JobService is required to persist Pix receipts."))
                            .CreateAsync(tenantId, new SettleChargeJob(chargeReceiptGroup.Key), transactionToken);

                    var commitResult = await unitOfWork.CommitAsync(transactionToken);
                    return commitResult.IsSuccess
                        ? NotificationResult<int>.Success(newlyInsertedReceiptCount)
                        : NotificationResult<int>.Failure(commitResult.Errors);
                },
                cancellationToken);
            if (groupPersistenceResult.IsFailure)
                return NotificationResult<PaymentReceiptPersistenceResult>.Failure(groupPersistenceResult.Errors);

            newlyStoredReceiptCount += groupPersistenceResult.Value;
        }

        return NotificationResult<PaymentReceiptPersistenceResult>.Success(new(
            receiptsWithTransactionId.Length,
            matchedReceipts.Count,
            matchedReceipts.Count - newlyStoredReceiptCount,
            receiptsWithTransactionId.Length - matchedReceipts.Count,
            newlyStoredReceiptCount));
    }

    private async Task<IReadOnlyList<MatchedPaymentReceipt>> MatchPaymentReceiptsAsync(
        Guid tenantId,
        Guid connectionId,
        string provider,
        IReadOnlyList<PixReceivedTransaction> receipts,
        CancellationToken cancellationToken)
    {
        var providerTransactionIds = receipts
            .Select(receipt => receipt.TxId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var attempts = await db.PaymentAttempts.AsNoTracking()
            .Where(attempt => attempt.TenantId == tenantId && attempt.ConnectionId == connectionId
                && attempt.Provider == provider && attempt.Status != "Canceled"
                && providerTransactionIds.Contains(attempt.ProviderExternalId))
            .Select(attempt => new { attempt.Id, attempt.ChargeId, attempt.ProviderExternalId })
            .ToDictionaryAsync(
                attempt => attempt.ProviderExternalId,
                StringComparer.Ordinal,
                cancellationToken);
        return receipts.Where(receipt => attempts.ContainsKey(receipt.TxId!)).Select(receipt =>
            new MatchedPaymentReceipt(
                tenantId,
                attempts[receipt.TxId!].ChargeId,
                attempts[receipt.TxId!].Id,
                connectionId,
                receipt)).ToArray();
    }

    private async Task LockChargeForReceiptAsync(Guid tenantId, Guid chargeId, CancellationToken cancellationToken)
    {
        new PostgresDatabaseConnection(db).RequireTransaction();
        // Serialize receipt insertion and Job lookup/creation for the same tenant/charge without a new index or table.
        var key = $"charge-settlement:{tenantId:D}:{chargeId:D}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);
    }

    private async Task<bool> PersistMatchedReceiptAsync(MatchedPaymentReceipt match, CancellationToken cancellationToken)
    {
        new PostgresDatabaseConnection(db).RequireTransaction();
        var receipt = match.Receipt;
        var existing = await db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.TenantId == match.TenantId
                && item.ConnectionId == match.BankConnectionId && item.EndToEndId == receipt.EndToEndId,
            cancellationToken);
        if (existing is not null)
        {
            if (existing.ChargeId != match.ChargeId || existing.PaymentAttemptId != match.PaymentAttemptId
                || existing.Amount != receipt.Amount || existing.ReceivedAt != receipt.ReceivedAt.UtcDateTime)
                throw new InvalidOperationException("A stored bank receipt conflicts with the repeated receipt.");
            return false;
        }
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = match.TenantId,
            ChargeId = match.ChargeId,
            PaymentAttemptId = match.PaymentAttemptId,
            ConnectionId = match.BankConnectionId,
            EndToEndId = receipt.EndToEndId,
            Amount = receipt.Amount,
            ReceivedAt = receipt.ReceivedAt.UtcDateTime,
            ProviderPayload = receipt.ProviderPayloadJson,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        });
        var audit = AuditWriteMetadata.Capture(actorContext, clock);
        var currentUtcTime = audit.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE finance.payment_attempt SET status = 'Succeeded', issuance_blocked = false,
                risk_closed_at = COALESCE(risk_closed_at, {currentUtcTime}),
                risk_closure_evidence = COALESCE(risk_closure_evidence, 'ProviderConfirmedReceipt'),
                failure_code = NULL, failure_message = NULL, updated_at = {audit.UtcNow}, updated_by = {audit.ActorId}
            WHERE tenant_id = {match.TenantId} AND id = {match.PaymentAttemptId} AND charge_id = {match.ChargeId}
              AND status NOT IN ('Succeeded', 'Canceled')
            """,
            cancellationToken);
        return true;
    }

    private Task FlushPaymentChangesAsync(CancellationToken cancellationToken)
    {
        new PostgresDatabaseConnection(db).RequireTransaction();
        return db.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> HasSettlementJobAsync(Guid tenantId, Guid chargeId, CancellationToken cancellationToken)
    {
        new PostgresDatabaseConnection(db).RequireTransaction();
        var key = new SettleChargeJob(chargeId).BuildKey();
        return db.Database.SqlQuery<int>($"""
            SELECT 1 AS "Value" FROM core.job
            WHERE tenant_id = {tenantId} AND type = {SettleChargeJob.JobType} AND key = {key}
            """).AnyAsync(cancellationToken);
    }

    private const string PaymentAttemptOperation = "CreatePaymentAttempt";
    private const string SupersededAttemptEvidence = "SupersededByAnotherPaymentMethod";
    private static readonly TimeSpan ProcessingRecoveryDelay = TimeSpan.FromMinutes(2);

    public async Task<PaymentAttemptReservation> ReserveAttemptAsync(
        string token,
        string methodCode,
        string idempotencyKey,
        CancellationToken cancellationToken, Guid? cardSubmissionId = null)
    {
        var request = await db.PaymentRequests.AsNoTracking()
            .SingleOrDefaultAsync(paymentRequest => paymentRequest.TokenHash == token, cancellationToken);
        if (request is null)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.NotFound,
                "Solicitação de pagamento não encontrada.");

        try
        {
            var reservation = await unitOfWork.ExecuteAsync(
                ct => ReserveAttemptAsync(request, methodCode, idempotencyKey, ct, cardSubmissionId),
                cancellationToken);
            if (!reservation.RetryReservation)
                return reservation;

            return await unitOfWork.ExecuteAsync(
                ct => ReserveAttemptAsync(request, methodCode, idempotencyKey, ct, cardSubmissionId),
                cancellationToken);
        }
        catch (DBConcurrencyException)
        {
            return PaymentAttemptReservation.Fail(
                ErrorCodes.Conflict,
                "Este pagamento já está sendo processado. Atualize a página para consultar a situação.");
        }
    }

    private async Task<PaymentAttemptReservation> ReserveAttemptAsync(
        E.PaymentRequest originalRequest,
        string methodCode,
        string idempotencyKey,
        CancellationToken cancellationToken, Guid? cardSubmissionId)
    {
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        var lockedChargeRows = await db.Charges.FromSqlInterpolated(
                $"SELECT * FROM finance.charge WHERE tenant_id = {originalRequest.TenantId} AND id = {originalRequest.ChargeId} FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        if (lockedChargeRows.Length == 0)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.NotFound,
                "Solicitação de pagamento não encontrada.");

        var request = await db.PaymentRequests.SingleOrDefaultAsync(
            paymentRequest => paymentRequest.TenantId == originalRequest.TenantId
                && paymentRequest.Id == originalRequest.Id
                && paymentRequest.TokenHash == originalRequest.TokenHash,
            cancellationToken);
        if (request is null)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.NotFound,
                "Solicitação de pagamento não encontrada.");

        if (request.Status != "Active" || request.ExpiresAt <= currentUtcTime)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.Conflict,
                "Esta solicitação de pagamento expirou ou foi cancelada.");

        var hasReceivedPayment = await db.PaymentTransactions.AsNoTracking().AnyAsync(
            transaction => transaction.TenantId == request.TenantId && transaction.ChargeId == request.ChargeId,
            cancellationToken);
        if (hasReceivedPayment)
            return PaymentAttemptReservation.Fail(ErrorCodes.Conflict, "Esta cobrança já recebeu pagamento.");

        var requestHash = Fingerprint(cardSubmissionId is null ? methodCode : $"{methodCode}:{cardSubmissionId:N}");
        var receipt = await db.PaymentCommandReceipts.AsNoTracking().SingleOrDefaultAsync(
            commandReceipt => commandReceipt.TenantId == request.TenantId
                && commandReceipt.ChargeId == request.ChargeId
                && commandReceipt.Operation == PaymentAttemptOperation
                && commandReceipt.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (receipt is not null)
        {
            if (receipt.PaymentRequestId != request.Id)
                return PaymentAttemptReservation.Fail(
                    ErrorCodes.Conflict,
                    "Esta chave de envio já foi usada em outra solicitação.");
            if (receipt.RequestHash != requestHash)
                return PaymentAttemptReservation.Fail(
                    ErrorCodes.Conflict,
                    "Esta chave de envio já foi usada com outra forma de pagamento.");

            var replayedAttempt = await FindAttemptAsync(receipt.TenantId, receipt.PaymentAttemptId!.Value, cancellationToken);
            if (replayedAttempt is null)
                return PaymentAttemptReservation.Fail(
                    ErrorCodes.Conflict,
                    "Não foi possível recuperar a tentativa de pagamento.");
            if (await NeedsProviderReconciliationAsync(replayedAttempt, currentUtcTime, cancellationToken))
            {
                var recoveredAttempt = await db.PaymentAttempts.SingleOrDefaultAsync(
                    paymentAttempt => paymentAttempt.TenantId == receipt.TenantId
                        && paymentAttempt.Id == replayedAttempt.Id,
                    cancellationToken);
                if (recoveredAttempt is null)
                    return PaymentAttemptReservation.Fail(
                        ErrorCodes.Conflict,
                        "Não foi possível recuperar a tentativa de pagamento.");

                var recoveredInstrument = await ReadAttemptInstrumentAsync(recoveredAttempt, cancellationToken);
                if (recoveredInstrument is null)
                    return PaymentAttemptReservation.Fail(ErrorCodes.ServiceUnavailable, "A modalidade desta forma de pagamento está indisponível.");
                await ReserveAttemptRecoveryAsync(recoveredAttempt, cancellationToken);
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
                return PaymentAttemptReservation.Recover(ToPaymentAttemptData(recoveredAttempt, recoveredInstrument));
            }

            var replayedPix = await FindAttemptPixAsync(receipt.TenantId, replayedAttempt.Id, cancellationToken);
            var replayedBoleto = await FindAttemptBoletoAsync(receipt.TenantId, replayedAttempt.Id, cancellationToken);
            return PaymentAttemptReservation.Replay(CreatePublicPaymentAttemptResult(replayedAttempt, replayedPix, replayedBoleto));
        }

        var authorizedMethods = await (
            from requestMethod in db.PaymentRequestMethods.AsNoTracking()
            join paymentMethod in db.PaymentMethods.AsNoTracking()
                on new
                {
                    requestMethod.TenantId,
                    requestMethod.PaymentMethodId
                }
                equals new
                {
                    paymentMethod.TenantId,
                    PaymentMethodId = paymentMethod.Id
                }
            where requestMethod.TenantId == request.TenantId
                && requestMethod.PaymentRequestId == request.Id
                && paymentMethod.Active
            select new
            {
                requestMethod.PaymentMethodId,
                paymentMethod.Code,
                paymentMethod.InstrumentType
            })
            .ToArrayAsync(cancellationToken);
        var method = authorizedMethods.SingleOrDefault(row => string.Equals(
            row.Code,
            methodCode,
            StringComparison.OrdinalIgnoreCase));
        if (method is null)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.BusinessRule,
                "Esta forma de pagamento não está disponível para a solicitação.");
        var instrument = ParsePaymentInstrument(method.InstrumentType);
        if (instrument is not (YallaFinance.Domain.Payments.PaymentInstrumentType.Pix
            or YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard
            or YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto)
            || instrument == YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard && cardSubmissionId == Guid.Empty)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.BusinessRule,
                "Esta forma de pagamento ainda não pode ser emitida neste checkout.");

        var activeAttempt = await db.PaymentAttempts
            .Where(paymentAttempt => paymentAttempt.TenantId == request.TenantId
                && paymentAttempt.ChargeId == request.ChargeId
                && paymentAttempt.IssuanceBlocked)
            .OrderByDescending(paymentAttempt => paymentAttempt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeAttempt is not null && activeAttempt.Status == "AwaitingPayment"
            && activeAttempt.ExpiresAt is { } pixExpiresAt && pixExpiresAt <= currentUtcTime)
        {
            activeAttempt.Status = "Expired";
            activeAttempt.IssuanceBlocked = false;
            activeAttempt.RiskClosedAt = currentUtcTime;
            activeAttempt.RiskClosureEvidence = "PixExpiredByProviderExpiry";
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
            return PaymentAttemptReservation.RetryAfterRiskClosure();
        }

        if (activeAttempt is not null && activeAttempt.PaymentRequestId != request.Id)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.Conflict,
                "Já existe uma tentativa de pagamento em andamento para esta cobrança.");

        if (activeAttempt is not null && activeAttempt.PaymentMethodId != method.PaymentMethodId)
        {
            activeAttempt.Status = "Canceled";
            activeAttempt.IssuanceBlocked = false;
            activeAttempt.RiskClosedAt = currentUtcTime;
            activeAttempt.RiskClosureEvidence = SupersededAttemptEvidence;
            activeAttempt.FailureCode = "SupersededByAnotherPaymentMethod";
            activeAttempt.FailureMessage = "Tentativa encerrada localmente ao selecionar outra forma de pagamento.";
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
            return PaymentAttemptReservation.RetryAfterRiskClosure();
        }

        if (activeAttempt is not null)
        {
            if (activeAttempt.PaymentRequestId != request.Id || activeAttempt.PaymentMethodId != method.PaymentMethodId)
                return PaymentAttemptReservation.Fail(
                    ErrorCodes.Conflict,
                    "Já existe uma tentativa de pagamento em andamento para esta cobrança.");

            if (await NeedsProviderReconciliationAsync(activeAttempt, currentUtcTime, cancellationToken))
            {
                var activeInstrument = await ReadAttemptInstrumentAsync(activeAttempt, cancellationToken);
                if (activeInstrument is null)
                    return PaymentAttemptReservation.Fail(ErrorCodes.ServiceUnavailable, "A modalidade desta forma de pagamento está indisponível.");
                await ReserveAttemptRecoveryAsync(activeAttempt, cancellationToken);
                db.PaymentCommandReceipts.Add(NewAttemptReceipt(
                    request,
                    activeAttempt.Id,
                    idempotencyKey,
                    requestHash,
                    currentUtcTime));
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
                return PaymentAttemptReservation.Recover(ToPaymentAttemptData(activeAttempt, activeInstrument));
            }

            var activePix = await FindAttemptPixAsync(request.TenantId, activeAttempt.Id, cancellationToken);
            var activeBoleto = await FindAttemptBoletoAsync(request.TenantId, activeAttempt.Id, cancellationToken);
            db.PaymentCommandReceipts.Add(NewAttemptReceipt(
                request,
                activeAttempt.Id,
                idempotencyKey,
                requestHash,
                currentUtcTime));
            PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
            return PaymentAttemptReservation.Replay(CreatePublicPaymentAttemptResult(activeAttempt, activePix, activeBoleto));
        }

        if (instrument == YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard && cardSubmissionId is null)
            return PaymentAttemptReservation.Fail(ErrorCodes.Validation, "Informe os dados do cartão para continuar.");

        var assignedConnections = await (
            from mapping in db.PaymentMethodConnections.AsNoTracking()
            join externalConnection in db.ExternalConnections.AsNoTracking()
                on new
                {
                    mapping.TenantId,
                    mapping.ConnectionId
                }
                equals new
                {
                    externalConnection.TenantId,
                    ConnectionId = externalConnection.Id
                }
            where mapping.TenantId == request.TenantId && mapping.PaymentMethodId == method.PaymentMethodId
            select new
            {
                mapping.ConnectionId,
                externalConnection.Provider,
                externalConnection.Group,
                ConnectionEnabled = externalConnection.Enabled,
                MappingEnabled = mapping.Enabled
            })
            .Take(2)
            .ToArrayAsync(cancellationToken);
        if (assignedConnections.Length == 0)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.ServiceUnavailable,
                "A forma de pagamento está indisponível no momento.");
        if (assignedConnections.Length > 1)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.Conflict,
                "A forma de pagamento está vinculada a mais de uma conexão. Revise a configuração.");

        var assignedConnection = assignedConnections[0];
        if (!assignedConnection.MappingEnabled || !assignedConnection.ConnectionEnabled
            || assignedConnection.Group != "PaymentProvider")
            return PaymentAttemptReservation.Fail(
                ErrorCodes.ServiceUnavailable,
                "A forma de pagamento está indisponível no momento.");

        var charge = await db.Charges.AsNoTracking()
            .Where(row => row.TenantId == request.TenantId && row.Id == request.ChargeId)
            .Where(PaymentChargeEligibilityQuery.IsFinanciallyValid())
            .SingleOrDefaultAsync(cancellationToken);
        if (charge is null)
            return PaymentAttemptReservation.Fail(
                ErrorCodes.Conflict,
                "O valor desta cobrança não está disponível para pagamento.");

        var attempt = new E.PaymentAttempt
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ChargeId = request.ChargeId,
            PaymentRequestId = request.Id,
            PublicId = Base64Url(RandomNumberGenerator.GetBytes(32)),
            PaymentMethodId = method.PaymentMethodId,
            ConnectionId = assignedConnection.ConnectionId,
            Provider = assignedConnection.Provider,
            Status = "Processing",
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            ProviderExternalId = Guid.NewGuid().ToString("N"),
            IssuanceBlocked = true,
            Amount = charge.Amount,
            CreatedAt = currentUtcTime,
            UpdatedAt = null
        };
        db.PaymentAttempts.Add(attempt);
        if (instrument == YallaFinance.Domain.Payments.PaymentInstrumentType.Pix)
            db.PaymentAttemptPixes.Add(new E.PaymentAttemptPix { PaymentAttemptId = attempt.Id, TenantId = request.TenantId });
        else if (instrument == YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard)
            db.PaymentAttemptCards.Add(new E.PaymentAttemptCard
            {
                PaymentAttemptId = attempt.Id, TenantId = request.TenantId, ConnectionId = attempt.ConnectionId
            });
        else
            db.PaymentAttemptBoletos.Add(new E.PaymentAttemptBoleto
            {
                PaymentAttemptId = attempt.Id, TenantId = request.TenantId, ConnectionId = attempt.ConnectionId
            });
        db.PaymentCommandReceipts.Add(NewAttemptReceipt(
            request,
            attempt.Id,
            idempotencyKey,
            requestHash,
            currentUtcTime));
        PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(cancellationToken));
        return PaymentAttemptReservation.New(ToPaymentAttemptData(attempt, instrument));
    }

    public async Task<bool> IsPaymentRequestActiveAsync(
        Guid tenantId,
        Guid requestId,
        CancellationToken cancellationToken) => await db.PaymentRequests.AsNoTracking().AnyAsync(
            paymentRequest => paymentRequest.TenantId == tenantId
                && paymentRequest.Id == requestId
                && paymentRequest.Status == "Active"
                && paymentRequest.ExpiresAt > clock.GetUtcNow().UtcDateTime,
            cancellationToken);

    public Task<bool> PersistPixInstrumentAsync(
        Guid tenantId,
        Guid attemptId,
        PixChargeSnapshot snapshot,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(
            async token =>
            {
                var paymentAttempt = await db.PaymentAttempts.SingleAsync(
                    item => item.TenantId == tenantId && item.Id == attemptId,
                    token);
                var pixRow = await db.PaymentAttemptPixes.SingleAsync(
                    item => item.TenantId == tenantId && item.PaymentAttemptId == attemptId,
                    token);
                paymentAttempt.Status = "AwaitingPayment";
                paymentAttempt.ExpiresAt = snapshot.ExpiresAt.UtcDateTime;
                paymentAttempt.FailureCode = null;
                paymentAttempt.FailureMessage = null;
                pixRow.Brcode = snapshot.CopyPaste;
                pixRow.Location = snapshot.Location;
                pixRow.LocationId = snapshot.LocationId;
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                return true;
            },
            cancellationToken);

    public Task<bool> MarkAttemptUnknownAsync(Guid attemptId, string failureCode, CancellationToken cancellationToken,
        string? failureMessage = null) =>
        UpdateAttemptAsync(
            attemptId,
            cancellationToken,
            paymentAttempt =>
            {
                paymentAttempt.Status = "Unknown";
                paymentAttempt.IssuanceBlocked = true;
                paymentAttempt.FailureCode = SafeFailureCode(failureCode) ?? "ProviderOutcomeUnknown";
                paymentAttempt.FailureMessage = SafeFailureMessage(failureMessage)
                    ?? "O resultado da emissão não foi confirmado.";
            });

    public Task<bool> MarkAttemptProviderNotFoundAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        return UpdateAttemptAsync(
            attemptId,
            cancellationToken,
            paymentAttempt =>
            {
                paymentAttempt.Status = "Failed";
                paymentAttempt.IssuanceBlocked = false;
                paymentAttempt.RiskClosedAt = currentUtcTime;
                paymentAttempt.RiskClosureEvidence = "ProviderConfirmedNotFound";
                paymentAttempt.FailureCode = "ProviderChargeNotFound";
                paymentAttempt.FailureMessage = "O provedor confirmou que não havia cobrança para esta tentativa.";
            });
    }

    public Task<bool> MarkAttemptRejectedAsync(Guid attemptId, string failureCode, CancellationToken cancellationToken,
        string? failureMessage = null)
    {
        var currentUtcTime = clock.GetUtcNow().UtcDateTime;
        return UpdateAttemptAsync(
            attemptId,
            cancellationToken,
            paymentAttempt =>
            {
                paymentAttempt.Status = "Failed";
                paymentAttempt.IssuanceBlocked = false;
                paymentAttempt.RiskClosedAt = currentUtcTime;
                paymentAttempt.RiskClosureEvidence = "ProviderRejectedBeforeInstrument";
                paymentAttempt.FailureCode = SafeFailureCode(failureCode) ?? "ProviderRejected";
                paymentAttempt.FailureMessage = SafeFailureMessage(failureMessage)
                    ?? "O provedor recusou a emissão antes de criar um instrumento pagável.";
            });
    }

    private Task<bool> UpdateAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken,
        Action<E.PaymentAttempt> update) => unitOfWork.ExecuteAsync(
            async token =>
            {
                var paymentAttempt = await db.PaymentAttempts.SingleOrDefaultAsync(
                    item => item.Id == attemptId,
                    token);
                if (paymentAttempt is null)
                    return false;
                update(paymentAttempt);
                PostgresUnitOfWork.GetCommitValueOrThrow(await unitOfWork.CommitAsync(token));
                return true;
            },
            cancellationToken);

    private Task<E.PaymentAttempt?> FindAttemptAsync(Guid tenantId, Guid attemptId, CancellationToken cancellationToken) =>
        db.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(
            paymentAttempt => paymentAttempt.TenantId == tenantId && paymentAttempt.Id == attemptId,
            cancellationToken);

    private Task<E.PaymentAttemptPix?> FindAttemptPixAsync(Guid tenantId, Guid attemptId, CancellationToken cancellationToken) =>
        db.PaymentAttemptPixes.AsNoTracking().SingleOrDefaultAsync(
            pix => pix.TenantId == tenantId && pix.PaymentAttemptId == attemptId,
            cancellationToken);

    private Task<E.PaymentAttemptBoleto?> FindAttemptBoletoAsync(Guid tenantId, Guid attemptId, CancellationToken cancellationToken) =>
        db.PaymentAttemptBoletos.AsNoTracking().SingleOrDefaultAsync(
            boleto => boleto.TenantId == tenantId && boleto.PaymentAttemptId == attemptId,
            cancellationToken);

    private static E.PaymentCommandReceipt NewAttemptReceipt(
        E.PaymentRequest request,
        Guid attemptId,
        string idempotencyKey,
        string requestHash,
        DateTime createdAt) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ChargeId = request.ChargeId,
            PaymentRequestId = request.Id,
            PaymentAttemptId = attemptId,
            Operation = PaymentAttemptOperation,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            CreatedAt = createdAt
        };

    private async Task<YallaFinance.Domain.Payments.PaymentInstrumentType?> ReadAttemptInstrumentAsync(E.PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        // The persisted instrument belongs to the attempt; later catalog edits cannot reroute recovery.
        var card = await db.PaymentAttemptCards.AsNoTracking().AnyAsync(
            item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, cancellationToken);
        var pix = await db.PaymentAttemptPixes.AsNoTracking().AnyAsync(
            item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, cancellationToken);
        var boleto = await db.PaymentAttemptBoletos.AsNoTracking().AnyAsync(
            item => item.TenantId == attempt.TenantId && item.PaymentAttemptId == attempt.Id, cancellationToken);
        if ((card ? 1 : 0) + (pix ? 1 : 0) + (boleto ? 1 : 0) != 1) return null;
        if (card) return YallaFinance.Domain.Payments.PaymentInstrumentType.CreditCard;
        return boleto ? YallaFinance.Domain.Payments.PaymentInstrumentType.Boleto : YallaFinance.Domain.Payments.PaymentInstrumentType.Pix;
    }

    private static YallaFinance.Domain.Payments.PaymentInstrumentType? ParsePaymentInstrument(string? value) =>
        Enum.TryParse<YallaFinance.Domain.Payments.PaymentInstrumentType>(value, out var instrument)
            && Enum.IsDefined(instrument) && instrument.ToString() == value ? instrument : null;

    private static PaymentAttemptData ToPaymentAttemptData(E.PaymentAttempt attempt, YallaFinance.Domain.Payments.PaymentInstrumentType? instrument) => new(
        attempt.Id,
        attempt.TenantId,
        attempt.ChargeId,
        attempt.PaymentRequestId,
        attempt.PaymentMethodId,
        attempt.ConnectionId,
        attempt.Provider,
        attempt.ProviderExternalId,
        attempt.Amount,
        attempt.Status,
        attempt.IssuanceBlocked,
        attempt.UpdatedAt,
        attempt.ExpiresAt)
    {
        InstrumentType = instrument,
        CreatedAt = attempt.CreatedAt
    };

    private static PublicPaymentAttemptResult CreatePublicPaymentAttemptResult(
        E.PaymentAttempt attempt,
        E.PaymentAttemptPix? pix,
        E.PaymentAttemptBoleto? boleto = null,
        string? status = null,
        bool? canRetry = null,
        string? message = null) => new(
            status ?? attempt.Status,
            attempt.Amount,
            attempt.ExpiresAt,
            pix?.Brcode,
            pix?.Location,
            canRetry ?? (attempt.Status == "Failed" && !attempt.IssuanceBlocked),
            message)
        {
            InstrumentType = boleto is not null ? "Boleto" : pix is not null ? "Pix" : null,
            Boleto = boleto is null ? null : new PublicBoletoDetails(
                boleto.IdentificationField,
                boleto.Barcode,
                boleto.BankSlipNumber,
                boleto.BankSlipUrl,
                boleto.InvoiceUrl)
        };

    internal async Task ReserveAttemptRecoveryAsync(E.PaymentAttempt attempt, CancellationToken cancellationToken)
    {
        var audit = AuditWriteMetadata.Capture(actorContext, clock);
        var recoveryCutoff = audit.UtcNow - ProcessingRecoveryDelay;
        // Called under the existing charge lock. Reacquiring recovery is an explicit
        // persisted operation, even when the attempt already has Processing status.
        var affected = await db.PaymentAttempts
            .Where(item => item.TenantId == attempt.TenantId && item.Id == attempt.Id && item.IssuanceBlocked
                && (item.Status == "Unknown" || (item.Status == "AwaitingPayment"
                    && (item.ExpiresAt == null || item.ExpiresAt > audit.UtcNow)) || (item.Status == "Processing"
                    && (item.UpdatedAt ?? item.CreatedAt) <= recoveryCutoff)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "Processing")
                .SetProperty(item => item.IssuanceBlocked, true)
                .SetProperty(item => item.UpdatedAt, audit.UtcNow)
                .SetProperty(item => item.UpdatedBy, audit.ActorId), cancellationToken);
        if (affected != 1)
            throw new DBConcurrencyException("The payment attempt is no longer available for recovery.");

        attempt.Status = "Processing";
        attempt.IssuanceBlocked = true;
        attempt.UpdatedAt = audit.UtcNow;
        attempt.UpdatedBy = audit.ActorId;
        // ExecuteUpdate bypasses tracking. Synchronize its four persisted values so
        // CommitAsync does not write them again or restore the former audit pair.
        var entry = db.Entry(attempt);
        foreach (var name in new[] { nameof(attempt.Status), nameof(attempt.IssuanceBlocked), nameof(attempt.UpdatedAt), nameof(attempt.UpdatedBy) })
        {
            var property = entry.Property(name);
            property.OriginalValue = property.CurrentValue;
            property.IsModified = false;
        }
    }

    internal static bool NeedsProviderReconciliation(E.PaymentAttempt attempt, DateTime currentUtcTime) =>
        attempt.Status == "Unknown"
        || (attempt.Status == "AwaitingPayment" && attempt.ExpiresAt is null)
        || (attempt.Status == "Processing" && (attempt.UpdatedAt ?? attempt.CreatedAt) <= currentUtcTime - ProcessingRecoveryDelay);

    private async Task<bool> NeedsProviderReconciliationAsync(
        E.PaymentAttempt attempt,
        DateTime currentUtcTime,
        CancellationToken cancellationToken)
    {
        if (NeedsProviderReconciliation(attempt, currentUtcTime))
            return true;
        if (attempt.Status != "AwaitingPayment" || attempt.ExpiresAt is not { } expiresAt
            || expiresAt <= currentUtcTime)
            return false;

        var instrument = await ReadAttemptInstrumentAsync(attempt, cancellationToken);
        if (instrument != YallaFinance.Domain.Payments.PaymentInstrumentType.Pix)
            return false;

        var pix = await FindAttemptPixAsync(attempt.TenantId, attempt.Id, cancellationToken);
        return string.IsNullOrWhiteSpace(pix?.Brcode);
    }

    private static string Fingerprint(string methodCode) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(methodCode.ToUpperInvariant())));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static string? SafeFailureCode(string? code) => code is { Length: > 0 and <= 80 }
        && code.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-')
            ? code
            : null;

    private static string? SafeFailureMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;
        var sanitized = new string(message.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > 1000 ? sanitized[..1000] : sanitized;
    }
}
