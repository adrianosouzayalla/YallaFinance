using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Auth.LoginAttemptPolicy;

namespace YallaFinance.WebApi.Security;

public enum AuthenticationAttemptKind
{
    Login,
    IntegrationToken
}

/// <summary>
/// Limits failed authentication attempts per e-mail and logs failed logins without the password.
/// Only failed attempts count, so a successful login is never blocked. An unknown login is neither counted nor blocked.
/// The limit comes from the company settings. This filter never refuses a login because of configuration: if the limit
/// cannot be read, the attempt proceeds uncounted and the failure is logged.
/// </summary>
public sealed class AuthenticationAttemptFilter(
    AttemptRateObserver rateObserver,
    ILoginAttemptPolicyReader policies,
    ILogger<AuthenticationAttemptFilter> logger,
    AuthenticationAttemptKind kind) : IAsyncActionFilter
{
    public const string TooManyAttemptsCode = "TOO_MANY_ATTEMPTS";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = FindEmailArgument(context.ActionArguments);
        if (email is null)
        {
            LogFailedAttempt(await next(), null, clientIp);
            return;
        }

        var policyResult = await policies.ReadAsync(email, context.HttpContext.RequestAborted);
        if (policyResult.IsFailure)
        {
            logger.LogWarning("Política de tentativas indisponível ({Kind}); login segue sem contagem: e-mail {MaskedEmail}, IP {ClientIp}.",
                kind, EmailMask.Mask(email), clientIp);
            LogFailedAttempt(await next(), email, clientIp);
            return;
        }

        var policy = policyResult.Value;
        if (policy is null)
        {
            LogFailedAttempt(await next(), email, clientIp);
            return;
        }

        var rule = new AttemptRateRule(policy.MaxFailedAttempts, policy.Window);
        // Shared by every endpoint that authenticates a user, so the limit is per account and not per endpoint.
        var key = $"e-mail|{email.ToLowerInvariant()}";
        var now = DateTimeOffset.UtcNow;
        if (rateObserver.HasReachedLimit(key, rule, now))
        {
            logger.LogWarning("Tentativas bloqueadas ({Kind}): e-mail {MaskedEmail}, IP {ClientIp}.", kind, EmailMask.Mask(email), clientIp);
            context.Result = new ObjectResult(new
            {
                errors = new[] { new NotificationError(TooManyAttemptsCode, "Muitas tentativas. Aguarde alguns minutos e tente novamente.") }
            })
            { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        var executed = await next();
        if (IsClientFailure(executed))
            rateObserver.RecordAttempt(key, rule, now);
        LogFailedAttempt(executed, email, clientIp);
    }

    private void LogFailedAttempt(ActionExecutedContext executed, string? email, string clientIp)
    {
        if (executed.Result is not ObjectResult failure || !IsClientFailure(executed))
            return;

        var errors = failure.Value?.GetType().GetProperty("errors")?.GetValue(failure.Value) as IEnumerable<NotificationError>;
        var reason = errors is null ? "desconhecido" : string.Join(", ", errors.Select(error => error.Message));
        logger.LogWarning(
            "Falha de autenticação ({Kind}): e-mail {MaskedEmail}, IP {ClientIp}, status {StatusCode}, motivo {Reason}.",
            kind, EmailMask.Mask(email), clientIp, failure.StatusCode, reason);
    }

    private static bool IsClientFailure(ActionExecutedContext executed) =>
        executed.Result is ObjectResult { StatusCode: >= 400 and < 500 };

    private static string? FindEmailArgument(IDictionary<string, object?> arguments) =>
        arguments.Values
            .Select(argument => argument?.GetType().GetProperty("Email")?.GetValue(argument) as string)
            .FirstOrDefault(email => !string.IsNullOrWhiteSpace(email))
            ?.Trim();
}
