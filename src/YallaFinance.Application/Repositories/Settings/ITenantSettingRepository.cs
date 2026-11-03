using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Settings;

public interface ITenantSettingRepository
{
    Task<IReadOnlyDictionary<string, string>> ReadGroupAsync(Guid tenant, string group, CancellationToken ct);
    Task SaveAsync(
        Guid tenant,
        string group,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken ct);
}
