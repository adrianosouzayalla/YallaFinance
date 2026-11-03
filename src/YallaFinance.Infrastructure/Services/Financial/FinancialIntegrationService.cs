using YallaFinance.Infrastructure.Providers.Financial;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Application.Repositories.Connections;


using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
namespace YallaFinance.Infrastructure.Services.Financial;

internal sealed class FinancialIntegrationService(
    ICompanyConnectionRepository connections,
    IProviderResolver<IFinancialProvider> resolver) : IFinancialIntegrationService
{
    public Task<FinancialCatalogResult<FinancialCustomerSummary>> FindCustomersByDocumentAsync(
        FinancialCustomerLookup query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = FinancialDocument.Normalize(query?.Document);
        if (query is null || query.TenantId == Guid.Empty || document is null)
            return Task.FromResult(FinancialCatalogResult<FinancialCustomerSummary>.Failed(FinancialCatalogError.Validation));
        return QueryFinancialCatalogAsync<FinancialCustomer, FinancialCustomerSummary>(
            query.TenantId,
            (provider, connection) => provider.FindCustomersByDocumentAsync(
                new(query.TenantId, connection.Provider, document, connection.Id), connection, cancellationToken),
            customer => new(customer.ProviderExternalId, customer.Name, customer.Document, customer.Email, customer.Phone, customer.Address),
            customer => customer is not null && !customer.IsDeleted && !customer.IsArchived
                && !string.IsNullOrWhiteSpace(customer.ProviderExternalId) && !string.IsNullOrWhiteSpace(customer.Name)
                && FinancialDocument.Normalize(customer.Document) == document,
            cancellationToken);
    }

    public Task<FinancialCatalogResult<FinancialCostCenterSummary>> ListCostCentersAsync(
        FinancialCatalogPage query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query is null || query.TenantId == Guid.Empty || query.Skip < 0 || query.Take is < 1 or > 500)
            return Task.FromResult(FinancialCatalogResult<FinancialCostCenterSummary>.Failed(FinancialCatalogError.Validation));
        return QueryFinancialCatalogAsync<FinancialCostCenter, FinancialCostCenterSummary>(
            query.TenantId,
            (provider, connection) => provider.ListCostCentersAsync(
                new(query.TenantId, connection.Provider, connection.Id, query.Skip, query.Take), connection, cancellationToken),
            center => new(center.ProviderExternalId, center.Name),
            center => center is not null && !string.IsNullOrWhiteSpace(center.ProviderExternalId) && !string.IsNullOrWhiteSpace(center.Name),
            cancellationToken);
    }

    public Task<FinancialCatalogResult<FinancialCategorySummary>> ListCategoriesAsync(
        FinancialCatalogPage query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query is null || query.TenantId == Guid.Empty || query.Skip < 0 || query.Take is < 1 or > 500)
            return Task.FromResult(FinancialCatalogResult<FinancialCategorySummary>.Failed(FinancialCatalogError.Validation));
        return QueryFinancialCatalogAsync<FinancialCategory, FinancialCategorySummary>(
            query.TenantId,
            (provider, connection) => provider.ListCategoriesAsync(
                new(query.TenantId, connection.Provider, connection.Id, query.Skip, query.Take), connection, cancellationToken),
            category => new(category.ProviderExternalId, category.Name, category.Kind switch
            {
                FinancialCategoryKind.Income => FinancialCategoryType.Income,
                FinancialCategoryKind.Expense => FinancialCategoryType.Expense,
                _ => throw new InvalidOperationException("Invalid category nature.")
            }),
            category => category is not null && !string.IsNullOrWhiteSpace(category.ProviderExternalId)
                && !string.IsNullOrWhiteSpace(category.Name) && category.Kind is FinancialCategoryKind.Income or FinancialCategoryKind.Expense,
            cancellationToken);
    }

    private async Task<FinancialCatalogResult<TResult>> QueryFinancialCatalogAsync<TItem, TResult>(
        Guid tenantId,
        Func<IFinancialProvider, ProviderConnection, Task<FinancialQueryResult<TItem>>> queryProvider,
        Func<TItem, TResult> mapItem,
        Func<TItem, bool> isValidItem,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectionRecord = await connections.GetEnabledRuntimeAsync(tenantId, "FinancialSystem", cancellationToken);
            if (connectionRecord is null || connectionRecord.Id == Guid.Empty || !connectionRecord.Enabled
                || connectionRecord.Group != "FinancialSystem" || string.IsNullOrWhiteSpace(connectionRecord.Provider))
                return FinancialCatalogResult<TResult>.Failed(FinancialCatalogError.ConfigurationUnavailable);
            var connection = ToProviderConnection(connectionRecord);
            var provider = resolver.Resolve(connection.Provider);
            var providerQueryResult = await queryProvider(provider, connection);
            cancellationToken.ThrowIfCancellationRequested();
            if (providerQueryResult is null)
                return FinancialCatalogResult<TResult>.Failed(FinancialCatalogError.InvalidResponse);
            if (!providerQueryResult.Success)
                return FinancialCatalogResult<TResult>.Failed(
                    MapFinancialProviderErrorToCatalogError(providerQueryResult.ErrorCode));
            if (providerQueryResult.Items is null || providerQueryResult.ErrorCode is not null
                || providerQueryResult.TotalCount < providerQueryResult.Items.Count
                || providerQueryResult.TotalCount < 0
                || providerQueryResult.Items.Any(item => !isValidItem(item)))
                return FinancialCatalogResult<TResult>.Failed(FinancialCatalogError.InvalidResponse);
            return FinancialCatalogResult<TResult>.Completed(
                connection.Id,
                providerQueryResult.Items.Select(mapItem).ToArray(),
                providerQueryResult.TotalCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IntegrationConfigurationException)
        {
            return FinancialCatalogResult<TResult>.Failed(FinancialCatalogError.ConfigurationUnavailable);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            return FinancialCatalogResult<TResult>.Failed(FinancialCatalogError.ProviderUnavailable);
        }
    }

    private static FinancialCatalogError MapFinancialProviderErrorToCatalogError(string? code) => code switch
    {
        "Validation" => FinancialCatalogError.Validation,
        "OperationNotSupported" => FinancialCatalogError.OperationNotSupported,
        "Unauthorized" => FinancialCatalogError.ProviderUnauthorized,
        "ProviderNotFound" => FinancialCatalogError.ProviderNotFound,
        "RateLimited" => FinancialCatalogError.RateLimited,
        "ProviderRejected" => FinancialCatalogError.ProviderRejected,
        "ProviderResponseInvalid" or "ProviderResponseInconsistent" => FinancialCatalogError.InvalidResponse,
        "CredentialMissing" or "InvalidCredentialFormat" or "ConnectionUnavailable" or "ProviderMismatch"
            or "ProviderNotRegistered" or "UnknownProviderSetting" or "BaseUrlMissing" or "InvalidBaseUrl"
            or "InvalidCompanyId" => FinancialCatalogError.ConfigurationUnavailable,
        _ => FinancialCatalogError.ProviderUnavailable
    };

    public async Task<CreateChargeResult> CreateChargeAsync(
        CreateChargeData data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (data is null || data.ChargeId == Guid.Empty || data.TenantId == Guid.Empty || data.Customer is null ||
            data.Customer.CustomerId == Guid.Empty || data.CategoryId == Guid.Empty || data.Amount <= 0 || decimal.Round(
                data.Amount,
                2) != data.Amount ||
            data.ScheduleDate == default || data.DueDate == default || data.AccrualDate == default || string.IsNullOrWhiteSpace(data.Description))
            return CreateChargeResult.Rejected("Validation");
        ProviderConnection? connection = null;
        try
        {
            var connectionRecord = data.ConnectionId is { } id ? await connections.GetRuntimeAsync(
                data.TenantId,
                id,
                cancellationToken)
                : await connections.GetEnabledRuntimeAsync(data.TenantId, "FinancialSystem", cancellationToken);
            connection = connectionRecord is null ? null : ToProviderConnection(connectionRecord);
            if (connection is null || !connection.Enabled || connection.Group != "FinancialSystem")
                return CreateChargeResult.Rejected("ConnectionUnavailable");
            if (data.ExpectedProvider is not null &&
                !string.Equals(
                    data.ExpectedProvider,
                    connection.Provider,
                    StringComparison.Ordinal))
                return CreateChargeResult.Rejected("ProviderChanged", connection.Id) with
                {
                    Provider = data.ExpectedProvider
                };
            var result = await resolver.Resolve(connection.Provider).CreateChargeAsync(
                data,
                connection,
                cancellationToken);
            return result with
            {
                Provider = connection.Provider
            };
        }
        catch (IntegrationConfigurationException exception)
        {
            return CreateChargeResult.Rejected(exception.Code, connection?.Id) with
            {
                Provider = connection?.Provider
            };
        }
        catch (Exception exception) when (connection is not null &&
            exception is HttpRequestException or IOException or OperationCanceledException)
        {
            return CreateChargeResult.Indeterminate(connection!.Id) with
            {
                Provider = connection.Provider
            };
        }
    }

    private static ProviderConnection ToProviderConnection(CompanyConnectionRuntime connection)
        => new(
            connection.Id,
            connection.TenantId,
            connection.Provider,
            connection.Name,
            connection.Enabled,
            connection.Group);
}
