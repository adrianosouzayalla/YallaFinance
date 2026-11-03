using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Settings;
using FluentValidation;
using System.Globalization;

namespace YallaFinance.Application.Features.Company;

public sealed record CompanyPaymentsSettings(string? MaximumLinkDays, string? PublicOrigin);
public sealed record CompanySessionSettings(string? SessionLifetimeMinutes);
public sealed record CompanyUserRegistrationSettings(string? InitialPassword);
public sealed record CompanyFinancialSettlementSettings(string? ReceiptTimeZoneId);

internal static class CompanyTenant
{
    public static bool TryGet(ICurrentTenantContext context, out Guid tenantId) => Guid.TryParse(context.TenantId, out tenantId) && tenantId != Guid.Empty;
    public static NotificationResult<T> Unauthorized<T>() => NotificationResult<T>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Selecione uma empresa em uma sessão válida."));
}
public sealed record GetCompanyPaymentsSettingsQuery : IRequest<NotificationResult<CompanyPaymentsSettings>>;
public sealed class GetCompanyPaymentsSettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanyPaymentsSettingsQuery, NotificationResult<CompanyPaymentsSettings>>
{
    public async Task<NotificationResult<CompanyPaymentsSettings>> Handle(GetCompanyPaymentsSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyPaymentsSettings>();
        var values = await repository.ReadGroupAsync(tenantId, "Payments", cancellationToken);
        return NotificationResult<CompanyPaymentsSettings>.Success(new(values.GetValueOrDefault("MaximumLinkDays"), values.GetValueOrDefault("PublicOrigin")));
    }
}
public sealed record GetCompanySessionSettingsQuery : IRequest<NotificationResult<CompanySessionSettings>>;
public sealed class GetCompanySessionSettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanySessionSettingsQuery, NotificationResult<CompanySessionSettings>>
{
    public async Task<NotificationResult<CompanySessionSettings>> Handle(GetCompanySessionSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanySessionSettings>();
        var values = await repository.ReadGroupAsync(tenantId, "Authentication", cancellationToken);
        return NotificationResult<CompanySessionSettings>.Success(new(values.GetValueOrDefault("SessionLifetimeMinutes")));
    }
}
public sealed record GetCompanyUserRegistrationSettingsQuery : IRequest<NotificationResult<CompanyUserRegistrationSettings>>;
public sealed class GetCompanyUserRegistrationSettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanyUserRegistrationSettingsQuery, NotificationResult<CompanyUserRegistrationSettings>>
{
    public async Task<NotificationResult<CompanyUserRegistrationSettings>> Handle(GetCompanyUserRegistrationSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyUserRegistrationSettings>();
        var values = await repository.ReadGroupAsync(tenantId, "UserRegistration", cancellationToken);
        return NotificationResult<CompanyUserRegistrationSettings>.Success(new(values.GetValueOrDefault("InitialPassword")));
    }
}
public sealed record GetCompanyFinancialSettlementSettingsQuery : IRequest<NotificationResult<CompanyFinancialSettlementSettings>>;
public sealed class GetCompanyFinancialSettlementSettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanyFinancialSettlementSettingsQuery, NotificationResult<CompanyFinancialSettlementSettings>>
{
    public async Task<NotificationResult<CompanyFinancialSettlementSettings>> Handle(GetCompanyFinancialSettlementSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyFinancialSettlementSettings>();
        var values = await repository.ReadGroupAsync(tenantId, "RegionalSettings", cancellationToken);
        return NotificationResult<CompanyFinancialSettlementSettings>.Success(new(values.GetValueOrDefault("DefaultTimeZoneId")));
    }
}

public sealed record SaveCompanyPaymentsSettingsCommand(string? MaximumLinkDays, string? PublicOrigin) : IRequest<NotificationResult<string>>;
public sealed record SaveCompanySessionSettingsCommand(string? SessionLifetimeMinutes) : IRequest<NotificationResult<string>>;
public sealed record SaveCompanyUserRegistrationSettingsCommand(string? InitialPassword) : IRequest<NotificationResult<string>>;
public sealed record SaveCompanyFinancialSettlementSettingsCommand(string? ReceiptTimeZoneId) : IRequest<NotificationResult<string>>;

public sealed class SaveCompanyPaymentsSettingsCommandValidator : AbstractValidator<SaveCompanyPaymentsSettingsCommand>
{
    public SaveCompanyPaymentsSettingsCommandValidator()
    {
        RuleFor(command => command.MaximumLinkDays)
            .Must(value => value is null || int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var days) && days is >= 1 and <= 365)
            .WithMessage("Informe um prazo entre 1 e 365 dias.");
        RuleFor(command => command.PublicOrigin)
            .Must(value => string.IsNullOrEmpty(value) || CompanySettingsValidation.IsValidPublicOrigin(value))
            .WithMessage("Informe uma origem pública HTTPS válida; localhost pode usar HTTP.");
    }
}

public sealed class SaveCompanySessionSettingsCommandValidator : AbstractValidator<SaveCompanySessionSettingsCommand>
{
    public SaveCompanySessionSettingsCommandValidator() =>
        RuleFor(command => command.SessionLifetimeMinutes)
            .Must(value => value is null || int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) && minutes is >= 1 and <= 1440)
            .WithMessage("Informe uma duração entre 1 e 1440 minutos.");
}

public sealed class SaveCompanyUserRegistrationSettingsCommandValidator : AbstractValidator<SaveCompanyUserRegistrationSettingsCommand>
{
    public SaveCompanyUserRegistrationSettingsCommandValidator() =>
        RuleFor(command => command.InitialPassword)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value) && value.Length <= 128)
            .WithMessage("A senha inicial deve ter entre 1 e 128 caracteres.");
}

public sealed class SaveCompanyFinancialSettlementSettingsCommandValidator : AbstractValidator<SaveCompanyFinancialSettlementSettingsCommand>
{
    public SaveCompanyFinancialSettlementSettingsCommandValidator() =>
        RuleFor(command => command.ReceiptTimeZoneId)
            .Must(value => value is null || CompanySettingsValidation.IsValidTimeZone(value))
            .WithMessage("Informe um fuso horário válido.");
}

internal static class CompanySettingsValidation
{
    public static bool IsValidPublicOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
        && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && uri.AbsolutePath == "/";

    public static bool IsValidTimeZone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

internal static class CompanySettingsWrite
{
    public static bool TryGetActor(ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext,
        out Guid tenantId, out string actor)
    {
        tenantId = default;
        actor = "";
        var session = sessionContext.Value;
        return Guid.TryParse(tenantContext.TenantId, out tenantId)
            && tenantId != Guid.Empty
            && session is not null
            && session.TenantId == tenantId
            && session.UserId != Guid.Empty
            && (actor = session.UserId.ToString()) is not "";
    }

    public static NotificationResult Unauthorized() => NotificationResult.Failure(
        new NotificationError(ErrorCodes.Unauthorized, "Selecione uma empresa em uma sessão válida."));

    public static NotificationResult<string> Invalid(string field, string message) => NotificationResult<string>.Failure(
        new NotificationError(ErrorCodes.Validation, message, field));

    public static Task<NotificationResult<string>> SaveAsync(Guid tenantId, string group,
        IReadOnlyDictionary<string, string?> values, ITenantSettingRepository repository,
        CancellationToken cancellationToken) => values.Count == 0
        ? Task.FromResult(NotificationResult<string>.Success(group))
        : SaveGroupAsync(tenantId, group, values, repository, cancellationToken);

    private static async Task<NotificationResult<string>> SaveGroupAsync(Guid tenantId, string group,
        IReadOnlyDictionary<string, string?> values, ITenantSettingRepository repository,
        CancellationToken cancellationToken)
    {
        await repository.SaveAsync(tenantId, group, values, cancellationToken);
        return NotificationResult<string>.Success(group);
    }
}

public sealed class SaveCompanyPaymentsSettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanyPaymentsSettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanyPaymentsSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);

        var values = new Dictionary<string, string?>();
        if (request.MaximumLinkDays is not null)
        {
            if (!int.TryParse(request.MaximumLinkDays, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var days) || days is < 1 or > 365)
                return CompanySettingsWrite.Invalid(nameof(request.MaximumLinkDays), "Informe um prazo entre 1 e 365 dias.");
            values.Add("MaximumLinkDays", request.MaximumLinkDays);
        }
        if (request.PublicOrigin is not null)
        {
            if (!string.IsNullOrEmpty(request.PublicOrigin) && !CompanySettingsValidation.IsValidPublicOrigin(request.PublicOrigin))
                return CompanySettingsWrite.Invalid(nameof(request.PublicOrigin), "Informe uma origem pública HTTPS válida; localhost pode usar HTTP.");
            values.Add("PublicOrigin", request.PublicOrigin);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, "Payments", values, repository, cancellationToken);
    }

}

public sealed class SaveCompanySessionSettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanySessionSettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanySessionSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        var values = new Dictionary<string, string?>();
        if (request.SessionLifetimeMinutes is not null)
        {
            if (!int.TryParse(request.SessionLifetimeMinutes, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var minutes) || minutes is < 1 or > 1440)
                return CompanySettingsWrite.Invalid(nameof(request.SessionLifetimeMinutes), "Informe uma duração entre 1 e 1440 minutos.");
            values.Add("SessionLifetimeMinutes", request.SessionLifetimeMinutes);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, "Authentication", values, repository, cancellationToken);
    }
}

public sealed class SaveCompanyUserRegistrationSettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanyUserRegistrationSettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanyUserRegistrationSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        var values = new Dictionary<string, string?>();
        if (request.InitialPassword is not null)
        {
            if (string.IsNullOrWhiteSpace(request.InitialPassword) || request.InitialPassword.Length > 128)
                return CompanySettingsWrite.Invalid(nameof(request.InitialPassword), "A senha inicial deve ter entre 1 e 128 caracteres.");
            values.Add("InitialPassword", request.InitialPassword);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, "UserRegistration", values, repository, cancellationToken);
    }
}

public sealed class SaveCompanyFinancialSettlementSettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanyFinancialSettlementSettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanyFinancialSettlementSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        var values = new Dictionary<string, string?>();
        if (request.ReceiptTimeZoneId is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ReceiptTimeZoneId))
                return CompanySettingsWrite.Invalid(nameof(request.ReceiptTimeZoneId), "Informe um fuso horário válido.");
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.ReceiptTimeZoneId); }
            catch (TimeZoneNotFoundException) { return CompanySettingsWrite.Invalid(nameof(request.ReceiptTimeZoneId), "Informe um fuso horário válido."); }
            catch (InvalidTimeZoneException) { return CompanySettingsWrite.Invalid(nameof(request.ReceiptTimeZoneId), "Informe um fuso horário válido."); }
            values.Add("DefaultTimeZoneId", request.ReceiptTimeZoneId);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, "RegionalSettings", values, repository, cancellationToken);
    }
}
