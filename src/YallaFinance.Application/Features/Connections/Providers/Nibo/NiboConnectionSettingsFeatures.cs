using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Features.Connections.Shared;
using YallaFinance.Application.Repositories.Connections;
using FluentValidation;
using MediatR;

namespace YallaFinance.Application.Features.Connections.Providers.Nibo;

internal static class NiboConnectionSettingsValidator
{
    private static readonly string[] ExpectedSettings =
    [
        "Connection.BaseUrl",
        "Connection.CompanyId",
        "Authentication.ApiToken"
    ];

    public static bool TryValidate(IReadOnlyDictionary<string, string?> flattened,
        out IReadOnlyDictionary<(string Group, string Key), string?> values, out NotificationError? error)
    {
        values = new Dictionary<(string Group, string Key), string?>();
        error = null;
        if (flattened.Keys.Any(key => !ExpectedSettings.Contains(key, StringComparer.Ordinal))
            || ExpectedSettings.Any(key => !flattened.ContainsKey(key)))
        {
            error = new(ErrorCodes.Validation, "Configurações do provider inválidas.", "settings");
            return false;
        }

        if (!IsWebAddress(flattened["Connection.BaseUrl"], allowHttpLoopback: true)
            || string.IsNullOrWhiteSpace(flattened["Authentication.ApiToken"])
            || flattened["Authentication.ApiToken"]!.Any(char.IsControl)
            || flattened["Connection.CompanyId"] is { Length: > 0 } companyId
                && (string.IsNullOrWhiteSpace(companyId) || companyId.Any(char.IsControl)))
        {
            error = new(ErrorCodes.Validation, "Uma ou mais configurações do provider são inválidas.", "settings");
            return false;
        }

        values = ExpectedSettings.ToDictionary(SplitKey, key => key == "Connection.CompanyId"
            && string.IsNullOrWhiteSpace(flattened[key]) ? null : flattened[key]);
        return true;
    }

    private static (string Group, string Key) SplitKey(string key)
    {
        var split = key.Split('.', 2);
        return (split[0], split[1]);
    }

    private static bool IsWebAddress(string? value, bool allowHttpLoopback) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || allowHttpLoopback && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

public sealed record SaveNiboConnectionSettingsCommand(Guid ConnectionId,
    IReadOnlyList<CompanyConnectionSettingInput> Settings) : IRequest<NotificationResult<Guid>>;

public sealed class SaveNiboConnectionSettingsCommandValidator : AbstractValidator<SaveNiboConnectionSettingsCommand>
{
    public SaveNiboConnectionSettingsCommandValidator()
    {
        RuleFor(command => command.ConnectionId)
            .NotEmpty()
            .WithMessage("Informe a conexão e suas configurações.");
        RuleFor(command => command.Settings)
            .NotNull()
            .WithMessage("Informe a conexão e suas configurações.");
    }
}

public sealed class SaveNiboConnectionSettingsCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository repository, TimeProvider clock)
    : IRequestHandler<SaveNiboConnectionSettingsCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveNiboConnectionSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        if (request.ConnectionId == Guid.Empty || request.Settings is null)
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation,
                "Informe a conexão e suas configurações."));
        if (!ConnectionSettingsValidator.TryFlattenSettings(request.Settings, out var flattened, out var flatteningError))
            return NotificationResult<Guid>.Failure(flatteningError!);
        if (!NiboConnectionSettingsValidator.TryValidate(flattened, out var settings, out var validationError))
            return NotificationResult<Guid>.Failure(validationError!);
        return await repository.SaveSettingsAsync(tenantId, request.ConnectionId, settings,
            clock.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
