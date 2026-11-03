using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Common.Context;


namespace YallaFinance.WebApi.Authentication;

public sealed class InteractiveSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IUserSessionService sessions, CurrentSessionContext current,
    CurrentExecutionActorContext actor)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "InteractiveSession";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is not null)
            return AuthenticateResult.NoResult();
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        // Idempotence exception belongs only to the authenticated logout action.
        var logout = Context.GetEndpoint()?.Metadata.GetMetadata<AllowRevokedSessionAttribute>() is not null;
        var session = await sessions.ValidateAsync(authorization[7..].Trim(), logout, Context.RequestAborted);
        if (session is null) return AuthenticateResult.Fail("Sessão inválida ou expirada.");
        current.Value = session;
        actor.SetAuthenticatedUser(session.UserId);
        var claims = new List<Claim>
        {
            new("sub", session.UserId.ToString()), new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new("sid", session.SessionId.ToString()), new("tenantId", session.TenantId.ToString()),
            new(ClaimTypes.Name, session.DisplayName), new("exp", session.ExpiresAt.ToUnixTimeSeconds().ToString())
        };
        if (session.Email is not null) claims.Add(new(ClaimTypes.Email, session.Email));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
            new ClaimsIdentity(claims, SchemeName)), SchemeName));
    }
}
