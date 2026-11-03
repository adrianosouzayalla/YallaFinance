using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(IAuthenticationService authentication)
    : MediatR.IRequestHandler<LoginCommand, NotificationResult<LoginResult>>
{
    public async Task<NotificationResult<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var authenticationResult = await authentication.AuthenticateAsync(
            new LoginRequest(
                request.Email,
                request.Password,
                request.TenantId),
            cancellationToken);
        if (authenticationResult.ErrorCode == "TENANT_SELECTION_REQUIRED")
            return NotificationResult<LoginResult>.Success(LoginResult.From(authenticationResult));

        return authenticationResult.Success
            ? NotificationResult<LoginResult>.Success(LoginResult.From(authenticationResult))
            : NotificationResult<LoginResult>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                authenticationResult.ErrorCode ?? "Credenciais inválidas."));
    }
}
