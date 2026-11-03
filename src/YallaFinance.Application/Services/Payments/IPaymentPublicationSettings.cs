using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments;

public interface IPaymentPublicationSettings
{
    Task<Contracts.PaymentTenantSettings> GetAsync(Guid tenant, CancellationToken ct);
}
