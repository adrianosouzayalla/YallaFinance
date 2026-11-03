using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Entities.Tenants;


namespace YallaFinance.Infrastructure.Mockup.Services;

internal static class CatalogJwtTokenIssuer
{
    public static AuthenticationResult Create(
        JwtAuthenticationOptions settings,
        string email,
        TenantOption active,
        IReadOnlyList<TenantOption> tenants)
    {
        var currentUtcTime = DateTime.UtcNow;
        var expiresAt = currentUtcTime.AddMinutes(Math.Max(1, settings.LifetimeMinutes));
        var claims = new List<Claim>
        {
            new("tenantId", active.Id),
            new(JwtRegisteredClaimNames.Sub, email),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Name, "Usuário Yalla")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            currentUtcTime,
            expiresAt,
            credentials);
        return new AuthenticationResult(
            true,
            new JwtSecurityTokenHandler().WriteToken(token),
            "Usuário Yalla",
            null,
            new DateTimeOffset(
                expiresAt,
                TimeSpan.Zero),
            active.Id,
            tenants);
    }
}
