using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Repositories.Connections;

public sealed partial class PostgresCompanyConnectionRepository : ICompanyConnectionRepository
{
    private readonly YallaFinanceDbContext database;
    private readonly IUnitOfWork unitOfWork;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<YallaFinance.Domain.Payments.PaymentInstrumentType>> paymentCapabilities;

    public PostgresCompanyConnectionRepository(YallaFinanceDbContext database, IUnitOfWork unitOfWork,
        IEnumerable<CompanyPaymentProviderCapabilities>? paymentProviders = null)
    {
        this.database = database;
        this.unitOfWork = unitOfWork;
        paymentCapabilities = (paymentProviders ?? []).ToDictionary(provider => provider.Provider, provider => provider.Instruments, StringComparer.Ordinal);
    }

    public Task<CompanyConnectionRuntime?> GetRuntimeAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken)
        => ConnectionQuery(tenantId, connectionId).SingleOrDefaultAsync(cancellationToken);

    public Task<CompanyConnectionRuntime?> GetEnabledRuntimeAsync(
        Guid tenantId,
        string group,
        CancellationToken cancellationToken)
        => EnabledQuery(tenantId, group).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CompanyConnectionRuntime>> ListEnabledRuntimeAsync(
        string group,
        CancellationToken cancellationToken)
        => await EnabledConnectionsQuery(group)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CompanyConnectionRuntimeSetting>> ListRuntimeSettingsAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken)
        => await SettingsQuery(tenantId, connectionId).ToListAsync(cancellationToken);

    public Task<string?> ResolveMappingAsync(
        Guid tenantId,
        Guid connectionId,
        string mappingType,
        string internalValue,
        CancellationToken cancellationToken)
        => MappingQuery(tenantId, connectionId, mappingType, internalValue).SingleOrDefaultAsync(cancellationToken);

    internal IQueryable<CompanyConnectionRuntime> ConnectionQuery(Guid tenantId, Guid connectionId)
        => database.ExternalConnections.AsNoTracking()
            .Where(connection => connection.TenantId == tenantId && connection.Id == connectionId)
            .Select(connection => new CompanyConnectionRuntime(
                connection.Id,
                connection.TenantId,
                connection.Provider,
                connection.Name,
                connection.Enabled,
                connection.Group));

    internal IQueryable<CompanyConnectionRuntime> EnabledQuery(Guid tenantId, string group)
        => database.ExternalConnections.AsNoTracking()
            .Where(connection => connection.TenantId == tenantId && connection.Enabled && connection.Group == group)
            .OrderBy(connection => connection.Id)
            .Select(connection => new CompanyConnectionRuntime(
                connection.Id,
                connection.TenantId,
                connection.Provider,
                connection.Name,
                connection.Enabled,
                connection.Group));

    internal IQueryable<CompanyConnectionRuntime> EnabledConnectionsQuery(string group)
        => database.ExternalConnections.AsNoTracking()
            .Where(connection => connection.Enabled && connection.Group == group)
            .OrderBy(connection => connection.TenantId)
            .ThenBy(connection => connection.Provider)
            .ThenBy(connection => connection.Id)
            .Select(connection => new CompanyConnectionRuntime(
                connection.Id,
                connection.TenantId,
                connection.Provider,
                connection.Name,
                connection.Enabled,
                connection.Group));

    internal IQueryable<CompanyConnectionRuntimeSetting> SettingsQuery(Guid tenantId, Guid connectionId)
        => database.ExternalConnectionSettings.AsNoTracking()
            .Where(setting => setting.TenantId == tenantId && setting.ConnectionId == connectionId)
            .OrderBy(setting => setting.Group)
            .ThenBy(setting => setting.Key)
            .Select(setting => new CompanyConnectionRuntimeSetting(
                setting.TenantId,
                setting.ConnectionId,
                setting.Group,
                setting.Key,
                setting.Value));

    internal IQueryable<string> MappingQuery(
        Guid tenantId,
        Guid connectionId,
        string mappingType,
        string internalValue)
        => database.ExternalConnectionMappings.AsNoTracking()
            .Where(mapping => mapping.TenantId == tenantId
                && mapping.ConnectionId == connectionId
                && mapping.MappingType == mappingType
                && mapping.InternalValue == internalValue)
            .Select(mapping => mapping.ExternalValue);

    public IQueryable<CompanyConnectionSummary> BuildListQuery(Guid tenantId, CompanyConnectionFilter filter)
    {
        var connections = database.ExternalConnections.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(filter.Search)) connections = connections.Where(x => x.Name.ToLower().Contains(filter.Search.ToLower()));
        if (filter.Provider is not null) connections = connections.Where(x => x.Provider == filter.Provider);
        if (filter.Group is not null) connections = connections.Where(x => x.Group == filter.Group);
        if (filter.Enabled is not null) connections = connections.Where(x => x.Enabled == filter.Enabled);
        return connections.Select(x => new CompanyConnectionSummary { Id = x.Id, Name = x.Name, Provider = x.Provider, Group = x.Group, Enabled = x.Enabled, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt });
    }

    public async Task<CompanyPage<CompanyConnectionSummary>> ListAsync(Guid tenantId, CompanyConnectionFilter filter, CancellationToken cancellationToken)
    {
        var query = BuildListQuery(tenantId, filter);
        var total = await query.CountAsync(cancellationToken);
        var ordered = Order(query, filter.SortBy, filter.SortDirection).ThenBy(x => x.Id);
        return new(await Page(ordered, filter.Page, filter.PageSize).ToListAsync(cancellationToken), filter.Page, filter.PageSize, total);
    }

    public async Task<CompanyConnectionDetail?> GetAsync(Guid tenantId, Guid connectionId, CompanyConnectionDetailFilter filter, CancellationToken cancellationToken)
    {
        var connection = await database.ExternalConnections.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == connectionId)
            .Select(x => new CompanyConnectionSummary(x.Id, x.Name, x.Provider, x.Group, x.Enabled, x.CreatedAt, x.UpdatedAt)).SingleOrDefaultAsync(cancellationToken);
        if (connection is null) return null;
        var supported = connection.Provider is "Nibo" or "Sicoob" or "Asaas";
        var settings = supported ? ProjectSettings(connection.Provider, await database.ExternalConnectionSettings.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ConnectionId == connectionId).ToListAsync(cancellationToken)) : [];
        var categories = Empty<CompanyConnectionMapping>(filter.CategoryPage, filter.PageSize);
        var accounts = Empty<CompanyConnectionMapping>(filter.ReceiptAccountPage, filter.PageSize);
        var methods = Empty<CompanyConnectionPaymentMethod>(filter.PaymentMethodPage, filter.PageSize);
        IReadOnlyList<CompanyPaymentMethodProviderConflict> paymentMethodProviderConflicts = [];
        if (connection.Provider == "Nibo")
        {
            categories = await ReadMappingsAsync(BuildMappingQuery(tenantId, connectionId, "Category"), filter.CategoryPage, filter.PageSize, filter.CategorySortBy, filter.CategorySortDirection, cancellationToken);
            accounts = await ReadMappingsAsync(BuildMappingQuery(tenantId, connectionId, "ReceiptAccount"), filter.ReceiptAccountPage, filter.PageSize, filter.ReceiptAccountSortBy, filter.ReceiptAccountSortDirection, cancellationToken);
        }
        if (connection.Group == "PaymentProvider" && paymentCapabilities.ContainsKey(connection.Provider))
        {
            var query = BuildPaymentMethodQuery(tenantId, connectionId);
            var total = await query.CountAsync(cancellationToken);
            methods = new(await Page(Order(query, filter.PaymentMethodSortBy, filter.PaymentMethodSortDirection).ThenBy(x => x.Id), filter.PaymentMethodPage, filter.PageSize).ToListAsync(cancellationToken), filter.PaymentMethodPage, filter.PageSize, total);
        }
        if (connection.Group == "PaymentProvider")
        {
            paymentMethodProviderConflicts = await (
                from binding in database.PaymentMethodConnections.AsNoTracking()
                join otherConnection in database.ExternalConnections.AsNoTracking()
                    on new { binding.TenantId, ConnectionId = binding.ConnectionId }
                    equals new { otherConnection.TenantId, ConnectionId = otherConnection.Id }
                where binding.TenantId == tenantId
                    && binding.ConnectionId != connectionId
                orderby binding.PaymentMethodId, otherConnection.Provider, otherConnection.Name
                select new CompanyPaymentMethodProviderConflict(
                    binding.PaymentMethodId,
                    otherConnection.Provider,
                    otherConnection.Name))
                .ToListAsync(cancellationToken);
        }
        var availableMethods = connection.Group == "PaymentProvider"
            ? await database.PaymentMethods.AsNoTracking().Where(method => method.TenantId == tenantId && method.Active)
                .OrderBy(method => method.InternalDescription ?? method.Name).ThenBy(method => method.Id)
                .Select(method => new CompanyPaymentMethodOption(method.Id, method.Code, method.Name, method.Active,
                    method.InstrumentType, method.InternalDescription,
                    !method.PaymentRequestMethods.Any() && !database.PaymentAttempts.Any(attempt => attempt.TenantId == tenantId && attempt.PaymentMethodId == method.Id)
                        && !database.PaymentMethodConnections.Any(link => link.TenantId == tenantId && link.PaymentMethodId == method.Id)))
                .ToArrayAsync(cancellationToken)
            : [];
        return new(connection, supported, settings, categories, accounts, methods)
        {
            PaymentMethodProviderConflicts = paymentMethodProviderConflicts,
            AvailablePaymentMethods = availableMethods,
            SupportedPaymentInstruments = connection.Group == "PaymentProvider" && paymentCapabilities.TryGetValue(connection.Provider, out var instruments)
                ? instruments.Select(instrument => instrument.ToString()).ToArray() : []
        };
    }

    public IQueryable<CompanyConnectionMapping> BuildMappingQuery(Guid tenantId, Guid connectionId, string mappingType)
    {
        var mappings = database.ExternalConnectionMappings.AsNoTracking().Where(x => x.TenantId == tenantId && x.ConnectionId == connectionId && x.MappingType == mappingType);
        // Convert the catalogue UUID to text; invalid persisted internal values must never be cast to UUID.
        var catalogue = mappingType == "Category"
            ? database.Categories.AsNoTracking().Where(x => x.TenantId == tenantId).Select(x => new { Value = x.Id.ToString(), x.Name, x.Active })
            : database.PaymentMethods.AsNoTracking().Where(x => x.TenantId == tenantId).Select(x => new { Value = x.Id.ToString(), x.Name, x.Active });
        return from mapping in mappings
               join item in catalogue on mapping.InternalValue.ToLower() equals item.Value into items
               from item in items.DefaultIfEmpty()
               select new CompanyConnectionMapping { Id = mapping.Id, MappingType = mapping.MappingType, InternalValue = mapping.InternalValue, InternalName = item == null ? null : item.Name,
                   InternalActive = item == null ? null : (bool?)item.Active, ExternalValue = mapping.ExternalValue, CreatedAt = mapping.CreatedAt, UpdatedAt = mapping.UpdatedAt };
    }

    public IQueryable<CompanyConnectionPaymentMethod> BuildPaymentMethodQuery(Guid tenantId, Guid connectionId) =>
        from link in database.PaymentMethodConnections.AsNoTracking().Where(x => x.TenantId == tenantId && x.ConnectionId == connectionId)
        join method in database.PaymentMethods.AsNoTracking().Where(x => x.TenantId == tenantId) on link.PaymentMethodId equals method.Id into methods
        from method in methods.DefaultIfEmpty()
        select new CompanyConnectionPaymentMethod { Id = link.Id, PaymentMethodId = link.PaymentMethodId, PaymentMethodName = method == null ? null : method.Name,
                   PaymentMethodActive = method == null ? null : (bool?)method.Active, Enabled = link.Enabled, CreatedAt = link.CreatedAt, UpdatedAt = link.UpdatedAt,
                   InstrumentType = method == null ? null : method.InstrumentType, InternalDescription = method == null ? null : method.InternalDescription };

    private static string[] GetProviderSettingDefinitions(string provider) => provider switch
        {
            "Nibo" => ["Connection.BaseUrl", "Connection.CompanyId", "Authentication.ApiToken"],
            "Sicoob" => ["Connection.Environment", "Connection.BaseUrl", "Authentication.ClientId", "Authentication.TokenUrl", "Authentication.Scopes", "Authentication.CertificatePem", "Authentication.PrivateKeyPem", "Authentication.PrivateKeyPassword", "Pix.ReceiverKey", "Pix.ReceivedLookbackDays", "Transport.RequestTimeoutSeconds"],
            "Asaas" => ["Connection.BaseUrl", "Authentication.ApiToken", "Pix.ReceivedLookbackDays", "Transport.RequestTimeoutSeconds"],
            _ => []
        };

    public static IReadOnlyList<CompanyConnectionSetting> ProjectSettings(string provider, IEnumerable<ExternalConnectionSetting> settings)
    {
        var definitions = GetProviderSettingDefinitions(provider);
        var values = settings.ToDictionary(x => (x.Group, x.Key), x => x.Value);
        return definitions.Select(definition =>
        {
            var parts = definition.Split('.', 2);
            var sensitive = parts[0] == "Authentication" && parts[1] is "ApiToken" or "ClientId" or "CertificatePem" or "PrivateKeyPem" or "PrivateKeyPassword";
            values.TryGetValue((parts[0], parts[1]), out var value);
            return new CompanyConnectionSetting(parts[0], parts[1], value, !string.IsNullOrWhiteSpace(value), sensitive);
        }).ToArray();
    }

    private static async Task<CompanyPage<CompanyConnectionMapping>> ReadMappingsAsync(IQueryable<CompanyConnectionMapping> query, int page, int pageSize, string sortBy, string direction, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        return new(await Page(Order(query, sortBy, direction).ThenBy(x => x.Id), page, pageSize).ToListAsync(cancellationToken), page, pageSize, total);
    }
    private static CompanyPage<T> Empty<T>(int page, int pageSize) => new([], page, pageSize, 0);
    public static IQueryable<T> Page<T>(IQueryable<T> query, int page, int pageSize)
    {
        var offset = ((long)page - 1) * pageSize;
        return offset > int.MaxValue ? query.Take(0) : query.Skip((int)offset).Take(pageSize);
    }
    public static IOrderedQueryable<T> Order<T>(IQueryable<T> query, string field, string direction)
    {
        var parameter = Expression.Parameter(typeof(T), "row");
        var property = Expression.Property(parameter, char.ToUpperInvariant(field[0]) + field[1..]);
        var key = Expression.Lambda(property, parameter);
        var expression = Expression.Call(typeof(Queryable), direction == "desc" ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy), [typeof(T), property.Type], query.Expression, Expression.Quote(key));
        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(expression);
    }
}
