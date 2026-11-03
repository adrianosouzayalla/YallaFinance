using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Payments.GeneratePaymentRequests;

public sealed class PaymentRequestBatchService(IPaymentRepository store)
{
    public async Task<NotificationResult<PaymentRequestBatchResult>> GenerateAsync(
        Guid tenant,
        GeneratePaymentRequestsData data,
        CancellationToken ct)
    {
        if (tenant == Guid.Empty)
            return CreateFailureResult(ErrorCodes.Unauthorized, "Sessão inválida.");
        if (data.Items is null || data.Items.Count is < 1 or > 100 || data.Items.Any(paymentRequestItem => paymentRequestItem.ChargeId == Guid.Empty
            || string.IsNullOrWhiteSpace(paymentRequestItem.IdempotencyKey) || paymentRequestItem.IdempotencyKey.Length > 200)
            || data.Items.Select(paymentRequestItem => paymentRequestItem.ChargeId).Distinct().Count() != data.Items.Count
            || data.PaymentMethodIds is null || data.PaymentMethodIds.Count == 0 || data.PaymentMethodIds.Contains(Guid.Empty)
            || data.PaymentMethodIds.Distinct().Count() != data.PaymentMethodIds.Count || data.Note?.Length > 2000)
            return CreateFailureResult(
                ErrorCodes.Validation,
                "Selecione de 1 a 100 cobranças distintas e confira o formulário.");
        var batchItemResults = new List<PaymentRequestBatchItemResult>();
        foreach (var paymentRequestItem in data.Items.OrderBy(paymentRequestItem => paymentRequestItem.ChargeId))
        {
            ct.ThrowIfCancellationRequested();
            var publicationResult = await store.PublishAsync(tenant, new(
                paymentRequestItem.ChargeId,
                data.PaymentMethodIds,
                data.ExpiresAt,
                data.Note,
                paymentRequestItem.IdempotencyKey),
                ct);
            var publicationError = publicationResult.Errors.FirstOrDefault();
            batchItemResults.Add(new(
                paymentRequestItem.ChargeId,
                publicationResult.Value,
                publicationError?.Code,
                publicationError?.Message));
        }
        return NotificationResult<PaymentRequestBatchResult>.Success(new(batchItemResults));
    }

    private static NotificationResult<PaymentRequestBatchResult> CreateFailureResult(
        string code,
        string message) =>
        NotificationResult<PaymentRequestBatchResult>.Failure(new NotificationError(code, message));
}
