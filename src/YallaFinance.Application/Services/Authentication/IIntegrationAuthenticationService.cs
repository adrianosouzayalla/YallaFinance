using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Services.Authentication;

public interface IIntegrationAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken);
}
