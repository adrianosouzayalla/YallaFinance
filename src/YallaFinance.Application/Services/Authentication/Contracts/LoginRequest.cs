
using YallaFinance.Application;

namespace YallaFinance.Application.Services.Authentication.Contracts;

public sealed record LoginRequest(string Email, string Password, string? TenantId = null);
