using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.Retry;
using YallaFinance.Application.Repositories.Settings;

namespace YallaFinance.Application.Features.Company;

public sealed record CompanyJobRetrySettings(IReadOnlyDictionary<string, int> Limits);

public sealed record GetCompanyJobRetrySettingsQuery : IRequest<NotificationResult<CompanyJobRetrySettings>>;

public sealed class GetCompanyJobRetrySettingsQueryHandler(ICurrentTenantContext context, ITenantSettingRepository repository)
    : IRequestHandler<GetCompanyJobRetrySettingsQuery, NotificationResult<CompanyJobRetrySettings>>
{
    public async Task<NotificationResult<CompanyJobRetrySettings>> Handle(GetCompanyJobRetrySettingsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyJobRetrySettings>();
        var values = await repository.ReadGroupAsync(tenantId, CompanyJobRetrySettingsKeys.Group, cancellationToken);
        var limits = JobRetryTypes.All.ToDictionary(
            type => type,
            type => JobRetryPolicy.ParseLimit(values.GetValueOrDefault(type)),
            StringComparer.Ordinal);
        return NotificationResult<CompanyJobRetrySettings>.Success(new(limits));
    }
}

public sealed record SaveCompanyJobRetrySettingsCommand(IReadOnlyDictionary<string, int?> Limits) : IRequest<NotificationResult<string>>;

public sealed class SaveCompanyJobRetrySettingsCommandValidator : AbstractValidator<SaveCompanyJobRetrySettingsCommand>
{
    public SaveCompanyJobRetrySettingsCommandValidator() =>
        RuleFor(command => command.Limits)
            .NotNull()
            .Must(limits => limits is null || limits.Keys.All(type => JobRetryTypes.All.Contains(type, StringComparer.Ordinal)))
            .WithMessage("Tipo de Job desconhecido para retry.")
            .Must(limits => limits is null || limits.Values.All(value => value is null || value is >= 0 and <= JobRetryTypes.MaximumRetries))
            .WithMessage($"Informe um número de retentativas entre 0 e {JobRetryTypes.MaximumRetries}.");
}

public sealed class SaveCompanyJobRetrySettingsCommandHandler(
    ICurrentTenantContext tenantContext, CurrentSessionContext sessionContext, ITenantSettingRepository repository)
    : IRequestHandler<SaveCompanyJobRetrySettingsCommand, NotificationResult<string>>
{
    public async Task<NotificationResult<string>> Handle(SaveCompanyJobRetrySettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<string>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var type in JobRetryTypes.All)
        {
            if (!request.Limits.TryGetValue(type, out var limit)) continue;
            // Valor nulo remove a configuracao, e o tipo volta ao padrao de zero retentativas.
            values[type] = limit?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return await CompanySettingsWrite.SaveAsync(tenantId, CompanyJobRetrySettingsKeys.Group, values, repository, cancellationToken);
    }
}

public static class CompanyJobRetrySettingsKeys
{
    public const string Group = "job_retry_limit";
}
