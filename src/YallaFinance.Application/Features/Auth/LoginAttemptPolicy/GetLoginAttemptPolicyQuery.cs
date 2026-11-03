using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Repositories.Settings;
using YallaFinance.Application.Repositories.Users;

namespace YallaFinance.Application.Features.Auth.LoginAttemptPolicy;

/// <summary>Failed-login limit that applies to an account: failures allowed inside the window.</summary>
public sealed record LoginFailurePolicy(int MaxFailedAttempts, TimeSpan Window);

/// <summary>
/// Reads the failed-login limit configured for the companies of the account identified by the login.
/// An unknown login has no account to protect, so it has no policy. When several companies apply, the most restrictive
/// values are used. A company without a valid value uses the default and a warning is logged. The query never fails.
/// </summary>
public sealed record GetLoginAttemptPolicyQuery(string Identifier) : IRequest<NotificationResult<LoginFailurePolicy?>>;

public sealed class GetLoginAttemptPolicyQueryValidator : AbstractValidator<GetLoginAttemptPolicyQuery>
{
    public GetLoginAttemptPolicyQueryValidator() =>
        RuleFor(query => query.Identifier).NotEmpty().WithMessage("Informe o login.");
}

public sealed class GetLoginAttemptPolicyQueryHandler(
    IUserIdentityRepository identities,
    ITenantSettingRepository settings,
    ILogger<GetLoginAttemptPolicyQueryHandler> logger)
    : IRequestHandler<GetLoginAttemptPolicyQuery, NotificationResult<LoginFailurePolicy?>>
{
    public Task<NotificationResult<LoginFailurePolicy?>> Handle(GetLoginAttemptPolicyQuery request, CancellationToken cancellationToken) =>
        identities.ExecuteLockedAsync(token => ReadPolicyAsync(request.Identifier, token), cancellationToken);

    private async Task<NotificationResult<LoginFailurePolicy?>> ReadPolicyAsync(string identifier, CancellationToken token)
    {
        var user = await identities.ResolveAsync(identifier, token);
        if (user is null)
            return NotificationResult<LoginFailurePolicy?>.Success(null);

        var tenants = await identities.TenantsAsync(user.Id, token);
        if (tenants.Count == 0)
            return NotificationResult<LoginFailurePolicy?>.Success(null);

        var maximumFailures = int.MaxValue;
        var longestWindowMinutes = 0;
        foreach (var tenant in tenants)
        {
            var (failures, minutes) = await ReadCompanyPolicyAsync(tenant.Id, token);
            maximumFailures = Math.Min(maximumFailures, failures);
            longestWindowMinutes = Math.Max(longestWindowMinutes, minutes);
        }

        return NotificationResult<LoginFailurePolicy?>.Success(
            new LoginFailurePolicy(maximumFailures, TimeSpan.FromMinutes(longestWindowMinutes)));
    }

    private async Task<(int Failures, int Minutes)> ReadCompanyPolicyAsync(string tenantId, CancellationToken token)
    {
        if (!Guid.TryParse(tenantId, out var tenant))
            return UsingDefaults(tenantId);

        var values = await settings.ReadGroupAsync(tenant, SecuritySettingRules.Group, token);
        var hasValidFailures = SecuritySettingRules.TryParseMaxFailedAttempts(values.GetValueOrDefault(SecuritySettingRules.MaxFailedAttemptsKey), out var failures);
        var hasValidMinutes = SecuritySettingRules.TryParseWindowMinutes(values.GetValueOrDefault(SecuritySettingRules.WindowMinutesKey), out var minutes);
        return hasValidFailures && hasValidMinutes ? (failures, minutes) : UsingDefaults(tenantId);
    }

    private (int Failures, int Minutes) UsingDefaults(string tenantId)
    {
        logger.LogWarning(
            "Configuração de segurança ausente ou inválida para a empresa {TenantId}; usando o padrão de {DefaultFailures} falhas em {DefaultMinutes} minutos.",
            tenantId, SecuritySettingRules.DefaultMaxFailedAttempts, SecuritySettingRules.DefaultWindowMinutes);
        return (SecuritySettingRules.DefaultMaxFailedAttempts, SecuritySettingRules.DefaultWindowMinutes);
    }
}
