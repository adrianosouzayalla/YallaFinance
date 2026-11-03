using FluentValidation;
using MediatR;
using System.Globalization;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Settings;

namespace YallaFinance.Application.Features.Company;

public sealed record CompanySecuritySettings(string? LoginMaxFailedAttempts, string? LoginAttemptWindowMinutes);

public sealed record GetCompanySecuritySettingsQuery : IRequest<NotificationResult<CompanySecuritySettings>>;

public sealed record SaveCompanySecuritySettingsCommand(string? LoginMaxFailedAttempts, string? LoginAttemptWindowMinutes)
    : IRequest<NotificationResult<string>>;

/// <summary>
/// Rules of the Security group. The bounds limit what a company may configure. The defaults apply only when a company has
/// no valid value, so a missing setting never blocks login; the reader logs a warning in that case.
/// </summary>
public static class SecuritySettingRules
{
    public const string Group = "Security";
    public const string MaxFailedAttemptsKey = "LoginMaxFailedAttempts";
    public const string WindowMinutesKey = "LoginAttemptWindowMinutes";
    public const int MinimumMaxFailedAttempts = 1;
    public const int MaximumMaxFailedAttempts = 100;
    public const int MinimumWindowMinutes = 1;
    public const int MaximumWindowMinutes = 1440;
    public const int DefaultMaxFailedAttempts = 10;
    public const int DefaultWindowMinutes = 15;
    public const string MaxFailedAttemptsMessage = "Informe um máximo de falhas entre 1 e 100.";
    public const string WindowMinutesMessage = "Informe uma janela entre 1 e 1440 minutos.";

    public static bool IsValidMaxFailedAttempts(string? value) => TryParseMaxFailedAttempts(value, out _);

    public static bool IsValidWindowMinutes(string? value) => TryParseWindowMinutes(value, out _);

    public static bool TryParseMaxFailedAttempts(string? value, out int parsed) =>
        TryParseInRange(value, MinimumMaxFailedAttempts, MaximumMaxFailedAttempts, out parsed);

    public static bool TryParseWindowMinutes(string? value, out int parsed) =>
        TryParseInRange(value, MinimumWindowMinutes, MaximumWindowMinutes, out parsed);

    private static bool TryParseInRange(string? value, int minimum, int maximum, out int parsed) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)
        && parsed >= minimum && parsed <= maximum;
}

public sealed class GetCompanySecuritySettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanySecuritySettingsQuery, NotificationResult<CompanySecuritySettings>>
{
    public async Task<NotificationResult<CompanySecuritySettings>> Handle(GetCompanySecuritySettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanySecuritySettings>();
        var values = await repository.ReadGroupAsync(tenantId, SecuritySettingRules.Group, cancellationToken);
        return NotificationResult<CompanySecuritySettings>.Success(new(
            values.GetValueOrDefault(SecuritySettingRules.MaxFailedAttemptsKey),
            values.GetValueOrDefault(SecuritySettingRules.WindowMinutesKey)));
    }
}

public sealed class SaveCompanySecuritySettingsCommandValidator : AbstractValidator<SaveCompanySecuritySettingsCommand>
{
    public SaveCompanySecuritySettingsCommandValidator()
    {
        RuleFor(command => command.LoginMaxFailedAttempts)
            .Must(value => value is null || SecuritySettingRules.IsValidMaxFailedAttempts(value))
            .WithMessage(SecuritySettingRules.MaxFailedAttemptsMessage);
        RuleFor(command => command.LoginAttemptWindowMinutes)
            .Must(value => value is null || SecuritySettingRules.IsValidWindowMinutes(value))
            .WithMessage(SecuritySettingRules.WindowMinutesMessage);
    }
}

public sealed class SaveCompanySecuritySettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanySecuritySettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanySecuritySettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);

        var values = new Dictionary<string, string?>();
        if (request.LoginMaxFailedAttempts is not null)
        {
            if (!SecuritySettingRules.IsValidMaxFailedAttempts(request.LoginMaxFailedAttempts))
                return CompanySettingsWrite.Invalid(nameof(request.LoginMaxFailedAttempts), SecuritySettingRules.MaxFailedAttemptsMessage);
            values.Add(SecuritySettingRules.MaxFailedAttemptsKey, request.LoginMaxFailedAttempts);
        }
        if (request.LoginAttemptWindowMinutes is not null)
        {
            if (!SecuritySettingRules.IsValidWindowMinutes(request.LoginAttemptWindowMinutes))
                return CompanySettingsWrite.Invalid(nameof(request.LoginAttemptWindowMinutes), SecuritySettingRules.WindowMinutesMessage);
            values.Add(SecuritySettingRules.WindowMinutesKey, request.LoginAttemptWindowMinutes);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, SecuritySettingRules.Group, values, repository, cancellationToken);
    }
}
