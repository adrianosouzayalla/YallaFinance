using System.Security.Claims;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.WebApi.Context;

public sealed class HttpCurrentTenantContext(IHttpContextAccessor accessor) : ICurrentTenantContext
{
    public string? TenantId => accessor.HttpContext?.User.FindFirstValue("tenantId");
}
