using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.PreparePayments;

public sealed class PreparePaymentsQueryHandler(
    IPaymentRepository reader,
    ICurrentTenantContext tenant,
    IPaymentPublicationSettings settings)
    : IRequestHandler<PreparePaymentsQuery, NotificationResult<PaymentPreparation>>
{
    public async Task<NotificationResult<PaymentPreparation>> Handle(
        PreparePaymentsQuery q,
        CancellationToken ct)
    {
        if (!Guid.TryParse(tenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return CreateFailureResult(
                ErrorCodes.Unauthorized,
                "Sessão inválida.");
        if (q.ChargeIds is null || q.ChargeIds.Count is < 1 or > 100 || q.ChargeIds.Contains(Guid.Empty)
            || q.ChargeIds.Distinct().Count() != q.ChargeIds.Count)
            return CreateFailureResult(
                ErrorCodes.Validation,
                "Selecione de 1 a 100 cobranças distintas.");
        var chargesToPrepare = new List<PaymentChargeSnapshot>();
        foreach (var chargeId in q.ChargeIds)
        {
            var chargeToPrepare = await reader.GetChargeAsync(tenantId, chargeId, ct);
            if (chargeToPrepare is null)
                return CreateFailureResult(ErrorCodes.NotFound, "Cobrança não encontrada.");
            chargesToPrepare.Add(chargeToPrepare);
        }
        var paymentPublicationSettings = await settings.GetAsync(tenantId, ct);
        return NotificationResult<PaymentPreparation>.Success(new(chargesToPrepare, await reader.AvailableMethodsAsync(
            tenantId,
            ct),
            paymentPublicationSettings.MaximumLinkDays));
    }

    private static NotificationResult<PaymentPreparation> CreateFailureResult(string code, string message) =>
        NotificationResult<PaymentPreparation>.Failure(new NotificationError(code, message));
}
