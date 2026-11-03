using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Auth.Login;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Integrations.Auth;

public sealed class IntegrationTokenCommandHandler(IIntegrationAuthenticationService authentication)
    : IRequestHandler<IntegrationTokenCommand, NotificationResult<LoginResult>>
{
    public async Task<NotificationResult<LoginResult>> Handle(
        IntegrationTokenCommand request,
        CancellationToken cancellationToken)
    {
        var authenticationResult = await authentication.AuthenticateAsync(
            new LoginRequest(
                request.Email,
                request.Password,
                request.TenantId),
            cancellationToken);
        if (authenticationResult.Success || authenticationResult.ErrorCode == "TENANT_SELECTION_REQUIRED")
            return NotificationResult<LoginResult>.Success(LoginResult.From(authenticationResult));

        return NotificationResult<LoginResult>.Failure(new NotificationError(
            ErrorCodes.Unauthorized,
            authenticationResult.ErrorCode ?? "Credenciais inválidas."));
    }
}
