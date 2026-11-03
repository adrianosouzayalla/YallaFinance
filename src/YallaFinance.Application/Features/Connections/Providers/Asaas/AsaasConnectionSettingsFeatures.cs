using System.Globalization;
using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Features.Connections.Shared;
using YallaFinance.Application.Repositories.Connections;

namespace YallaFinance.Application.Features.Connections.Providers.Asaas;

internal static class AsaasConnectionSettingsValidator
{
    private static readonly string[] ExpectedSettings =
    [
        "Connection.BaseUrl",
        "Authentication.ApiToken",
        "Pix.ReceivedLookbackDays",
        "Transport.RequestTimeoutSeconds"
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

        var token = flattened["Authentication.ApiToken"];
        var lookback = flattened["Pix.ReceivedLookbackDays"];
        if (!Uri.TryCreate(flattened["Connection.BaseUrl"], UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || !uri.IsDefaultPort || uri.Host is not ("api-sandbox.asaas.com" or "api.asaas.com")
            || uri.AbsolutePath.TrimEnd('/') != "/v3" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || string.IsNullOrWhiteSpace(token) || token.Any(character => char.IsControl(character) || char.IsWhiteSpace(character))
            || !int.TryParse(flattened["Transport.RequestTimeoutSeconds"], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || seconds is < 60 or > 300
            || !string.IsNullOrWhiteSpace(lookback)
                && (!int.TryParse(lookback, NumberStyles.None, CultureInfo.InvariantCulture, out var days) || days is < 1 or > 31))
        {
            error = new(ErrorCodes.Validation, "Confira a URL, o token, o timeout e as configura\u00e7\u00f5es Pix.", "settings");
            return false;
        }

        values = ExpectedSettings.ToDictionary(SplitKey, key => IsOptional(key) && string.IsNullOrWhiteSpace(flattened[key])
            ? null
            : flattened[key]);
        return true;
    }

    private static bool IsOptional(string key) => key == "Pix.ReceivedLookbackDays";

    private static (string Group, string Key) SplitKey(string key)
    {
        var split = key.Split('.', 2);
        return (split[0], split[1]);
    }
}

public sealed record SaveAsaasConnectionSettingsCommand(Guid ConnectionId,
    IReadOnlyList<CompanyConnectionSettingInput> Settings) : IRequest<NotificationResult<Guid>>;

public sealed class SaveAsaasConnectionSettingsCommandValidator : AbstractValidator<SaveAsaasConnectionSettingsCommand>
{
    public SaveAsaasConnectionSettingsCommandValidator()
    {
        RuleFor(command => command.ConnectionId).NotEmpty().WithMessage("Informe a conexão e suas configurações.");
        RuleFor(command => command.Settings).NotNull().WithMessage("Informe a conexão e suas configurações.");
    }
}

public sealed class SaveAsaasConnectionSettingsCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository repository, TimeProvider clock)
    : IRequestHandler<SaveAsaasConnectionSettingsCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveAsaasConnectionSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        if (request.ConnectionId == Guid.Empty || request.Settings is null)
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe a conexão e suas configurações."));
        if (!ConnectionSettingsValidator.TryFlattenSettings(request.Settings, out var flattened, out var flatteningError))
            return NotificationResult<Guid>.Failure(flatteningError!);
        if (!AsaasConnectionSettingsValidator.TryValidate(flattened, out var settings, out var validationError))
            return NotificationResult<Guid>.Failure(validationError!);
        var connection = await repository.GetRuntimeAsync(tenantId, request.ConnectionId, cancellationToken);
        if (connection is null || connection.TenantId != tenantId || connection.Id != request.ConnectionId
            || connection.Provider != "Asaas" || connection.Group != "PaymentProvider")
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.NotFound, "Conexão Asaas não encontrada."));
        return await repository.SaveSettingsAsync(tenantId, request.ConnectionId, settings, clock.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
