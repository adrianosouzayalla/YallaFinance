using YallaFinance.Application.Repositories.IntegrationOperations.Contracts;
using YallaFinance.Domain.Integrations;

namespace YallaFinance.Application.Repositories.IntegrationOperations;

public interface IIntegrationOperationRepository
{
    Task<IntegrationOperation?> FindAsync(
        Guid tenantId,
        string type,
        string externalId,
        CancellationToken token);
    Task<IntegrationOperationSnapshot?> GetAsync(Guid tenantId, Guid operationId, CancellationToken token);
    Task LockAsync(Guid tenantId, string type, string externalId, CancellationToken token);
    Task AddAsync(IntegrationOperation operation, CancellationToken token);
}
