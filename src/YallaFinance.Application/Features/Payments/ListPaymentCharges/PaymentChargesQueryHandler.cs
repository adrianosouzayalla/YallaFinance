using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;


using YallaFinance.Domain.Charges;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Payments.ListPaymentCharges;

public sealed class PaymentChargesQueryHandler(IPaymentRepository reader, ICurrentTenantContext tenant)
    : IRequestHandler<PaymentChargesQuery, NotificationResult<PaymentChargeListResult>>
{
    public async Task<NotificationResult<PaymentChargeListResult>> Handle(
        PaymentChargesQuery q,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return NotificationResult<PaymentChargeListResult>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão inválida."));
        if (q.Page is < 1 or > 1_000_000 || q.PageSize is < 1 or > 100 || q.StartDate > q.EndDate
            || q.EndDate == DateOnly.MaxValue || q.SortDirection is not ("asc" or "desc")
            || q.SortBy is not (null or "charge" or "customer" or "contract" or "created" or "due" or "amount" or "eligibility")
            || q.PaymentStatus is not (null or "awaitingPayment" or "expired" or "available"))
            return NotificationResult<PaymentChargeListResult>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Confira o período e a paginação."));
        if (q.Reference is not null && !ChargeReference.TryParse(q.Reference, out _, out _))
            return NotificationResult<PaymentChargeListResult>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Informe a cobrança no formato empresa-número, como 10-123."));
        return NotificationResult<PaymentChargeListResult>.Success(await reader.ListAsync(
            tenantId,
            new(q.StartDate, q.EndDate, q.Page, q.PageSize, q.SortBy, q.SortDirection, q.PaymentStatus, q.Reference),
            ct));
    }
}
