using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application.Repositories.PaymentMethods;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.PaymentMethods.ListPaymentMethods;

public sealed class ListPaymentMethodsQueryHandler(
    IPaymentMethodRepository repository,
    ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<ListPaymentMethodsQuery, NotificationResult<IReadOnlyList<PaymentMethodResponse>>>
{
    public async Task<NotificationResult<IReadOnlyList<PaymentMethodResponse>>> Handle(
        ListPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<IReadOnlyList<PaymentMethodResponse>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        return NotificationResult<IReadOnlyList<PaymentMethodResponse>>.Success(await repository.ListAsync(
            currentTenant.TenantId,
            cancellationToken));
    }
}
