using YallaFinance.Application;
namespace YallaFinance.Application.Common.Context;

public interface ICurrentTenantContext
{
    string? TenantId
    {
        get;
    }
}
