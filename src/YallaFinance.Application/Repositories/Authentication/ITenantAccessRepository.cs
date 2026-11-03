using YallaFinance.Application.Entities.Tenants;

namespace YallaFinance.Application.Repositories.Authentication;

public interface ITenantAccessRepository
{
    Task<IReadOnlyList<TenantOption>> GetAuthorizedTenantsAsync(string email, CancellationToken cancellationToken);
    Task<bool> IsTenantAvailableAsync(Guid tenantId, CancellationToken cancellationToken);
}
