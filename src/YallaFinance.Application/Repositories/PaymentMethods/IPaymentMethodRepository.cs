using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.PaymentMethods;

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethodResponse>> ListAsync(string tenantId, CancellationToken cancellationToken);
}
