using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Services.Charges;

public interface IPaymentService
{
    Task<ChargeResult> CreateAsync(
        ChargeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
