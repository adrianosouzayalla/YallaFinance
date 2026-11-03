using System.Security.Claims;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.WebApi.Context;

public sealed class HttpCurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        && userId != Guid.Empty ? userId : null;
    public string? Email => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? accessor.HttpContext?.User.Identity?.Name;
}
