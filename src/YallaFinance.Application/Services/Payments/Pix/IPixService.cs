using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Application.Services.Payments.Pix;

public interface IPixService
{
    Task<CreatePixChargeResult> CreateAsync(CreatePixChargeData data, CancellationToken cancellationToken);
    Task<PixChargeQueryResult> GetAsync(PixChargeQuery query, CancellationToken cancellationToken);
    Task<PixReceivedQueryResult> QueryAsync(PixReceivedQuery query, CancellationToken cancellationToken);
}
