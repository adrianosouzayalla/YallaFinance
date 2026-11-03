using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using YallaFinance.Infrastructure.Services.Financial;
using System.Net;
using System.Text.Json;
using Refit;

using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    public Task<FinancialQueryResult<FinancialCustomer>> ListCustomersAsync(
        FinancialCatalogQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => ReadCatalogAsync<NiboCustomerResponse, FinancialCustomer>(
            query,
            connection,
            (niboApi, accessToken) => niboApi.ListCustomersAsync(
                query.Skip,
                query.Take,
                "id",
                accessToken,
                cancellationToken),
            customer =>
            {
                if (customer is null ||
                    string.IsNullOrWhiteSpace(customer.Id) ||
                    string.IsNullOrWhiteSpace(customer.Name) ||
                    customer.IsDeleted is null ||
                    customer.IsArchived is null)
                    throw new JsonException();
                return new(
                    customer.Id,
                    customer.Name,
                    FinancialDocument.Normalize(customer.Document?.Number) ?? customer.Document?.Number,
                    customer.IsDeleted.Value,
                    customer.IsArchived.Value,
                    customer.ContactEmail,
                    customer.ContactPhone,
                    customer.IsDeleted.Value || customer.IsArchived.Value ? null : customer.Address?.ToCustomerAddress());
            },
            cancellationToken);

    public Task<FinancialQueryResult<FinancialCostCenter>> ListCostCentersAsync(
        FinancialCatalogQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => ReadCatalogAsync<NiboCostCenterResponse, FinancialCostCenter>(
            query,
            connection,
            (niboApi, accessToken) => niboApi.ListCostCentersAsync(
                query.Skip,
                query.Take,
                "description",
                accessToken,
                cancellationToken),
            costCenter =>
            {
                if (costCenter is null ||
                    string.IsNullOrWhiteSpace(costCenter.Id) ||
                    string.IsNullOrWhiteSpace(costCenter.Description))
                    throw new JsonException();
                return new(costCenter.Id, costCenter.Description);
            },
            cancellationToken);

    public Task<FinancialQueryResult<FinancialCategory>> ListCategoriesAsync(
        FinancialCatalogQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => ReadCatalogAsync<NiboCategoryResponse, FinancialCategory>(
            query,
            connection,
            (niboApi, accessToken) => niboApi.ListCategoriesAsync(
                query.Skip,
                query.Take,
                "id",
                accessToken,
                cancellationToken),
            category =>
            {
                if (category is null ||
                    string.IsNullOrWhiteSpace(category.Id) ||
                    string.IsNullOrWhiteSpace(category.Name) ||
                    category.Type is not ("in" or "out"))
                    throw new JsonException();
                return new(
                    category.Id,
                    category.Name,
                    category.Type == "in" ? FinancialCategoryKind.Income : FinancialCategoryKind.Expense);
            },
            cancellationToken);

    public async Task<FinancialQueryResult<FinancialCustomer>> FindCustomersByDocumentAsync(
        FinancialCustomerLookupData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        if (FinancialDocument.Normalize(data.Document) != data.Document)
            return FinancialQueryResult<FinancialCustomer>.Failed("Validation");
        for (var attempt = 0; ; attempt++)
        {
            var result = await FindCustomersByDocumentAttemptAsync(data, connection, cancellationToken);
            if (result.Success ||
                attempt == 2 ||
                result.ErrorCode is not ("ProviderResponseInconsistent" or "ProviderResponseInvalid"))
                return result;
            await Task.Delay(250 * (attempt + 1) + Random.Shared.Next(250), cancellationToken);
        }
    }

    private async Task<FinancialQueryResult<FinancialCustomer>> FindCustomersByDocumentAttemptAsync(
        FinancialCustomerLookupData data,
        ProviderConnection connection,
        CancellationToken cancellationToken,
        bool fullCatalog = false)
    {
        var matchingCustomers = new List<FinancialCustomer>();
        var seenCustomerIds = new HashSet<string>(StringComparer.Ordinal);
        var query = new FinancialCatalogQuery(data.TenantId, data.Provider, data.ConnectionId, Take: 500);
        int? totalCount = null;
        // Consulta o documento nas formas normalizada e pontuada.
        var document = data.Document;
        var formattedDocument = document.Length == 11
            ? $"{document[..3]}.{document[3..6]}.{document[6..9]}-{document[9..]}"
            : $"{document[..2]}.{document[2..5]}.{document[5..8]}/{document[8..12]}-{document[12..]}";
        var filter = $"document/number eq '{document}' or document/number eq '{formattedDocument}'";
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = fullCatalog ? await ListCustomersAsync(query, connection, cancellationToken) : await ReadCatalogAsync<NiboCustomerResponse, FinancialCustomer>(
                query,
                connection,
                (niboApi, accessToken) => niboApi.FindCustomersAsync(
                    filter,
                    query.Skip,
                    query.Take,
                    "id",
                    accessToken,
                    cancellationToken),
                customer =>
                {
                    if (customer is null || string.IsNullOrWhiteSpace(customer.Id) || string.IsNullOrWhiteSpace(customer.Name) ||
                        customer.IsDeleted is null || customer.IsArchived is null || FinancialDocument.Normalize(customer.Document?.Number) != document)
                        throw new JsonException();
                    return new(
                        customer.Id,
                        customer.Name,
                        document,
                        customer.IsDeleted.Value,
                        customer.IsArchived.Value,
                        customer.ContactEmail,
                        customer.ContactPhone,
                        customer.IsDeleted.Value || customer.IsArchived.Value ? null : customer.Address?.ToCustomerAddress());
                },
                cancellationToken);
            if (!page.Success)
                return FinancialQueryResult<FinancialCustomer>.Failed(page.ErrorCode!);
            if (totalCount is not null && totalCount != page.TotalCount)
                return FinancialQueryResult<FinancialCustomer>.Failed("ProviderResponseInconsistent");
            totalCount = page.TotalCount;
            foreach (var customer in page.Items)
            {
                if (!seenCustomerIds.Add(customer.ProviderExternalId))
                    return FinancialQueryResult<FinancialCustomer>.Failed("ProviderResponseInconsistent");
                if (!customer.IsDeleted &&
                    !customer.IsArchived &&
                    FinancialDocument.Normalize(customer.Document) == data.Document)
                    matchingCustomers.Add(customer);
            }
            query = query with
            {
                Skip = query.Skip + page.Items.Count
            };
            if (query.Skip >= page.TotalCount)
            {
                // Confirma a ausência no catálogo completo para reconhecer documentos legados.
                if (!fullCatalog && matchingCustomers.Count == 0)
                    return await FindCustomersByDocumentAttemptAsync(
                        data,
                        connection,
                        cancellationToken,
                        fullCatalog: true);
                return FinancialQueryResult<FinancialCustomer>.Completed(
                    matchingCustomers.ToArray(),
                    matchingCustomers.Count);
            }
        }
    }

    private async Task<FinancialQueryResult<T>> ReadCatalogAsync<TResponse, T>(
        FinancialCatalogQuery query,
        ProviderConnection connection,
        Func<INiboApi, string, Task<HttpResponseMessage>> execute,
        Func<TResponse, T> map,
        CancellationToken cancellationToken,
        string? diagnosticOperation = null)
    {
        var options = await configuration.ReadAsync(connection, cancellationToken);
        using var client = clients.Create(options);
        var niboApi = RestService.For<INiboApi>(
            client,
            new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)) });
        cancellationToken.ThrowIfCancellationRequested();
        int? httpStatusCode = null;
        FinancialProviderDiagnostic? CreateReadDiagnostic(Exception? exception = null) => diagnosticOperation is null ? null
            : CreateFinancialProviderDiagnostic(diagnosticOperation, "GET", "/receipts", httpStatusCode, exception);
        try
        {
            using var response = await SendReadAsync(
                () => execute(niboApi, options.ApiToken),
                options.ApiToken,
                cancellationToken);
            httpStatusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
                return FinancialQueryResult<T>.Failed(
                    MapHttpStatusToErrorCode(response.StatusCode),
                    CreateReadDiagnostic());
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = JsonSerializer.Deserialize<NiboListResponse<TResponse>>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (page?.Items is null || page.Count is null or < 0 || page.Items.Count > query.Take ||
                (page.Items.Count > 0 && (long)query.Skip + page.Items.Count > page.Count) || (query.Skip < page.Count && page.Items.Count == 0))
                return FinancialQueryResult<T>.Failed("ProviderResponseInvalid", CreateReadDiagnostic());
            return FinancialQueryResult<T>.Completed(page.Items.Select(map).ToArray(), page.Count.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or ApiException or ApiRequestException)
        {
            return FinancialQueryResult<T>.Failed("ProviderUnavailable", CreateReadDiagnostic(exception));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException)
        {
            return FinancialQueryResult<T>.Failed("ProviderResponseInvalid", CreateReadDiagnostic(exception));
        }
    }
}
