using YallaFinance.Application.Common.Results;

namespace YallaFinance.Application.Features.Connections.Shared;

internal static class ConnectionSettingsValidator
{
    public static bool TryFlattenSettings(IReadOnlyList<CompanyConnectionSettingInput> settings,
        out IReadOnlyDictionary<string, string?> flattened, out NotificationError? error)
    {
        flattened = new Dictionary<string, string?>();
        error = null;
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var setting in settings)
        {
            if (setting is null || string.IsNullOrWhiteSpace(setting.Group) || string.IsNullOrWhiteSpace(setting.Key)
                || !values.TryAdd(setting.Group + "." + setting.Key, setting.Value))
            {
                error = new(ErrorCodes.Validation, "Configurações do provider inválidas.", nameof(settings));
                return false;
            }
        }

        flattened = values;
        return true;
    }
}
