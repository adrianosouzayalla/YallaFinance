using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.GeneratePaymentRequests;

public sealed class GeneratePaymentRequestsCommandHandler(
    PaymentRequestBatchService service,
    ICurrentTenantContext tenant,
    ICurrentUserContext user)
    : IRequestHandler<GeneratePaymentRequestsCommand, NotificationResult<PaymentRequestBatchResult>>
{
    public Task<NotificationResult<PaymentRequestBatchResult>> Handle(
        GeneratePaymentRequestsCommand q,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenant.TenantId, out var tenantId) ||
            tenantId == Guid.Empty ||
            user.UserId is null || user.UserId == Guid.Empty)
            return Task.FromResult(NotificationResult<PaymentRequestBatchResult>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão inválida.")));
        // The store replays receipts before checking token protection; checkout availability is independent.
        return service.GenerateAsync(tenantId, q.Data, ct);
    }
}
