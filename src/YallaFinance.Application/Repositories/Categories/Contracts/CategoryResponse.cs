using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Categories.Contracts;

public sealed record CategoryResponse(string Id, string Code, string Name, string TenantId, bool Active);
