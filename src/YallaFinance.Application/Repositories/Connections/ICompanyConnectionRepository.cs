namespace YallaFinance.Application.Repositories.Connections;

using YallaFinance.Application.Common.Results;
using YallaFinance.Domain.Payments;

public sealed record CompanyPaymentProviderCapabilities(string Provider, IReadOnlyList<PaymentInstrumentType> Instruments);
public sealed record CompanyPaymentMethodConfiguration(PaymentInstrumentType? InstrumentType, string? InternalDescription);
public sealed record CompanyPaymentMethodOption(Guid Id, string Code, string Name, bool Active,
    string? InstrumentType, string? InternalDescription, bool CanClassify);

public interface ICompanyConnectionRepository
{
    Task<CompanyConnectionRuntime?> GetRuntimeAsync(Guid tenantId, Guid connectionId, CancellationToken cancellationToken);
    Task<CompanyConnectionRuntime?> GetEnabledRuntimeAsync(Guid tenantId, string group, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyConnectionRuntime>> ListEnabledRuntimeAsync(string group, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyConnectionRuntimeSetting>> ListRuntimeSettingsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken);
    Task<string?> ResolveMappingAsync(
        Guid tenantId,
        Guid connectionId,
        string mappingType,
        string internalValue,
        CancellationToken cancellationToken);
    Task<CompanyPage<CompanyConnectionSummary>> ListAsync(Guid tenantId, CompanyConnectionFilter filter, CancellationToken cancellationToken);
    Task<CompanyConnectionDetail?> GetAsync(Guid tenantId, Guid connectionId, CompanyConnectionDetailFilter filter, CancellationToken cancellationToken);
    Task<NotificationResult<Guid>> SaveConnectionAsync(Guid tenantId, Guid? connectionId,
        string name, string provider, string group, bool enabled, DateTime now,
        CancellationToken cancellationToken);
    Task<NotificationResult<Guid>> SaveSettingsAsync(Guid tenantId, Guid connectionId,
        IReadOnlyDictionary<(string Group, string Key), string?> settings, DateTime now,
        CancellationToken cancellationToken);
    Task<NotificationResult<Guid>> SaveMappingAsync(Guid tenantId, Guid connectionId, Guid? mappingId,
        string mappingType, Guid internalId, string externalValue, DateTime now,
        CancellationToken cancellationToken);
    Task<NotificationResult<Guid>> RemoveMappingAsync(Guid tenantId, Guid connectionId, Guid mappingId,
        CancellationToken cancellationToken);
    Task<NotificationResult<Guid>> SavePaymentMethodAsync(Guid tenantId, Guid connectionId, Guid? linkId,
        Guid paymentMethodId, bool enabled, DateTime now,
        CancellationToken cancellationToken, CompanyPaymentMethodConfiguration? configuration = null);
    Task<NotificationResult<Guid>> RemovePaymentMethodAsync(Guid tenantId, Guid connectionId, Guid linkId,
        CancellationToken cancellationToken);
}

public sealed record CompanyConnectionRuntimeSetting(
    Guid TenantId,
    Guid ConnectionId,
    string Group,
    string Key,
    string? Value);

public sealed record CompanyConnectionRuntime(
    Guid Id,
    Guid TenantId,
    string Provider,
    string Name,
    bool Enabled,
    string Group);

public sealed record CompanyPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems);
public sealed record CompanyConnectionSummary(Guid Id, string Name, string Provider, string Group, bool Enabled, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public CompanyConnectionSummary() : this(default, "", "", "", false, default, default) { }
}
public sealed record CompanyConnectionSetting(string Group, string Key, string? Value, bool Configured, bool Sensitive);
public sealed record CompanyConnectionMapping(Guid Id, string MappingType, string InternalValue, string? InternalName, bool? InternalActive, string ExternalValue, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public CompanyConnectionMapping() : this(default, "", "", null, null, "", default, default) { }
}
public sealed record CompanyConnectionPaymentMethod(Guid Id, Guid PaymentMethodId, string? PaymentMethodName, bool? PaymentMethodActive, bool Enabled, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public CompanyConnectionPaymentMethod() : this(default, default, null, null, false, default, default) { }
    public string? InstrumentType { get; init; }
    public string? InternalDescription { get; init; }
}
public sealed record CompanyPaymentMethodProviderConflict(Guid PaymentMethodId, string Provider, string ConnectionName);
public sealed record CompanyConnectionDetail(CompanyConnectionSummary Connection, bool ProviderSupported, IReadOnlyList<CompanyConnectionSetting> Settings,
    CompanyPage<CompanyConnectionMapping> CategoryMappings, CompanyPage<CompanyConnectionMapping> ReceiptAccountMappings, CompanyPage<CompanyConnectionPaymentMethod> PaymentMethods)
{
    public IReadOnlyList<CompanyPaymentMethodProviderConflict> PaymentMethodProviderConflicts { get; init; } = [];
    public IReadOnlyList<string> SupportedPaymentInstruments { get; init; } = [];
    public IReadOnlyList<CompanyPaymentMethodOption> AvailablePaymentMethods { get; init; } = [];
}

public sealed record CompanyConnectionFilter
{
    public string? Search { get; init; }
    public string? Provider { get; init; }
    public string? Group { get; init; }
    public bool? Enabled { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "name";
    public string SortDirection { get; init; } = "asc";
}

public sealed record CompanyConnectionDetailFilter
{
    public int CategoryPage { get; init; } = 1;
    public int ReceiptAccountPage { get; init; } = 1;
    public int PaymentMethodPage { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string CategorySortBy { get; init; } = "internalName";
    public string CategorySortDirection { get; init; } = "asc";
    public string ReceiptAccountSortBy { get; init; } = "internalName";
    public string ReceiptAccountSortDirection { get; init; } = "asc";
    public string PaymentMethodSortBy { get; init; } = "paymentMethodName";
    public string PaymentMethodSortDirection { get; init; } = "asc";
}
