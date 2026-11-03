using YallaFinance.Application.Services.Payments.Boleto.Contracts;

namespace YallaFinance.Application.Services.Payments.Boleto;

public interface IBoletoService
{
    Task<BoletoOperationResult<BoletoCharge>> CreateAsync(CreateBoletoChargeData data, CancellationToken cancellationToken);
    Task<BoletoOperationResult<BoletoCharge>> GetAsync(BoletoChargeQuery query, CancellationToken cancellationToken);
}
