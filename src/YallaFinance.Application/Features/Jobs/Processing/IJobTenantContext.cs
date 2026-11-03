using YallaFinance.Application.Common.Context;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobTenantContext : ICurrentTenantContext
{
    void Initialize(Guid tenantId);
}
