using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobTenantContext : IJobTenantContext
{
    public string? TenantId
    {
        get; private set;
    }

    public void Initialize(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (TenantId is not null)
            throw new InvalidOperationException("A processing scope has only one tenant.");
        TenantId = tenantId.ToString();
    }
}
