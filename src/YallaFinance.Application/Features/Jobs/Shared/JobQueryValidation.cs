using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared;

public static class JobQueryValidation
{
    public static void Tenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
    }

    public static int Offset(Guid tenantId, int page, int pageSize)
    {
        Tenant(tenantId);
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        var requestedOffset = ((long)page - 1) * pageSize;
        if (requestedOffset > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page));
        return (int)requestedOffset;
    }
}
