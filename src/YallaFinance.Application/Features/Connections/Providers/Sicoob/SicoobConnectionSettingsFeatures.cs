using System.Globalization;
using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Features.Connections.Shared;
using YallaFinance.Application.Repositories.Connections;

namespace YallaFinance.Application.Features.Connections.Providers.Sicoob;

internal static class SicoobConnectionSettingsValidator
{
    private static readonly string[] ExpectedSettings =
    [
        "Connection.Environment",
        "Connection.BaseUrl",
        "Authentication.ClientId",
        "Authentication.TokenUrl",
        "Authentication.Scopes",
        "Authentication.CertificatePem",
        "Authentication.PrivateKeyPem",
        "Authentication.PrivateKeyPassword",
        "Pix.ReceiverKey",
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

        var clientId = flattened["Authentication.ClientId"];
        var scopes = flattened["Authentication.Scopes"];
        var certificate = flattened["Authentication.CertificatePem"];
        var privateKey = flattened["Authentication.PrivateKeyPem"];
        var timeout = flattened["Transport.RequestTimeoutSeconds"];
        var receiverKey = flattened["Pix.ReceiverKey"];
        var lookback = flattened["Pix.ReceivedLookbackDays"];
        var valid = flattened["Connection.Environment"] == "Production"
            && IsExactHttpsAddress(flattened["Connection.BaseUrl"], "api.sicoob.com.br", "/pix/api/v2")
            && !string.IsNullOrWhiteSpace(clientId) && clientId.All(character => character is >= (char)33 and <= (char)126)
            && IsExactHttpsAddress(flattened["Authentication.TokenUrl"], "auth.sicoob.com.br", "/auth/realms/cooperado/protocol/openid-connect/token")
            && !string.IsNullOrWhiteSpace(scopes) && !scopes.Any(char.IsControl)
            && scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("cob.read", StringComparer.Ordinal)
            && scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("cob.write", StringComparer.Ordinal)
            && !string.IsNullOrWhiteSpace(certificate) && certificate.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(privateKey) && privateKey.Contains("PRIVATE KEY-----", StringComparison.Ordinal)
            && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var timeoutSeconds) && timeoutSeconds is >= 1 and <= 60
            && (string.IsNullOrWhiteSpace(receiverKey) || receiverKey.Length <= 77 && !receiverKey.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)))
            && (string.IsNullOrWhiteSpace(lookback) || int.TryParse(lookback, NumberStyles.None, CultureInfo.InvariantCulture, out var days) && days is >= 1 and <= 31);

        if (!valid)
        {
            error = new(ErrorCodes.Validation, "Uma ou mais configurações do provider são inválidas.", "settings");
            return false;
        }

        values = ExpectedSettings.ToDictionary(SplitKey, key => IsOptional(key) && string.IsNullOrWhiteSpace(flattened[key])
            ? null
            : flattened[key]);
        return true;
    }

    private static bool IsOptional(string key) => key is "Authentication.PrivateKeyPassword" or "Pix.ReceiverKey" or "Pix.ReceivedLookbackDays";

    private static (string Group, string Key) SplitKey(string key)
    {
        var split = key.Split('.', 2);
        return (split[0], split[1]);
    }

    private static bool IsExactHttpsAddress(string? value, string host, string path) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
        && string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.AbsolutePath.TrimEnd('/'), path, StringComparison.Ordinal);
}

public sealed record SaveSicoobConnectionSettingsCommand(Guid ConnectionId,
    IReadOnlyList<CompanyConnectionSettingInput> Settings) : IRequest<NotificationResult<Guid>>;

public sealed class SaveSicoobConnectionSettingsCommandValidator : AbstractValidator<SaveSicoobConnectionSettingsCommand>
{
    public SaveSicoobConnectionSettingsCommandValidator()
    {
        RuleFor(command => command.ConnectionId)
            .NotEmpty()
            .WithMessage("Informe a conexão e suas configurações.");
        RuleFor(command => command.Settings)
            .NotNull()
            .WithMessage("Informe a conexão e suas configurações.");
    }
}

public sealed class SaveSicoobConnectionSettingsCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository repository, TimeProvider clock)
    : IRequestHandler<SaveSicoobConnectionSettingsCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveSicoobConnectionSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        if (request.ConnectionId == Guid.Empty || request.Settings is null)
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation,
                "Informe a conexão e suas configurações."));
        if (!ConnectionSettingsValidator.TryFlattenSettings(request.Settings, out var flattened, out var flatteningError))
            return NotificationResult<Guid>.Failure(flatteningError!);
        if (!SicoobConnectionSettingsValidator.TryValidate(flattened, out var settings, out var validationError))
            return NotificationResult<Guid>.Failure(validationError!);
        return await repository.SaveSettingsAsync(tenantId, request.ConnectionId, settings,
            clock.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
