using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using System.Net;
using System.Text.Json;
using Refit;

using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;
using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    public async Task<FinancialQueryResult<FinancialReceivable>> GetReceivableAsync(
        FinancialReceivableIdQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(query.ProviderExternalId, out var scheduleId) ||
            scheduleId == Guid.Empty)
        {
            return FinancialQueryResult<FinancialReceivable>.Failed("Validation");
        }

        var niboOptions = await configuration.ReadAsync(connection, cancellationToken);
        using var httpClient = clients.Create(niboOptions);
        var niboApi = RestService.For<INiboApi>(httpClient);
        cancellationToken.ThrowIfCancellationRequested();
        int? httpStatusCode = null;

        try
        {
            using var response = await SendReadAsync(
                () => niboApi.GetReceivableAsync(
                    scheduleId,
                    niboOptions.ApiToken,
                    cancellationToken),
                niboOptions.ApiToken,
                cancellationToken);
            httpStatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return FinancialQueryResult<FinancialReceivable>.Failed(
                    MapHttpStatusToErrorCode(response.StatusCode),
                    CreateFinancialProviderDiagnostic("GetReceivable", "GET", "/schedules/credit/{scheduleId}", httpStatusCode));
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var receivableResponse = JsonSerializer.Deserialize<NiboReceivableResponse>(
                responseBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var receivable = MapReceivableResponse(receivableResponse);

            if (receivable.ProviderExternalId != scheduleId.ToString())
            {
                return FinancialQueryResult<FinancialReceivable>.Failed(
                    "ProviderResponseInvalid",
                    CreateFinancialProviderDiagnostic("GetReceivable", "GET", "/schedules/credit/{scheduleId}", httpStatusCode));
            }

            return FinancialQueryResult<FinancialReceivable>.Completed([receivable], 1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or ApiException or ApiRequestException)
        {
            return FinancialQueryResult<FinancialReceivable>.Failed(
                "ProviderUnavailable",
                CreateFinancialProviderDiagnostic("GetReceivable", "GET", "/schedules/credit/{scheduleId}", httpStatusCode, exception));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException)
        {
            return FinancialQueryResult<FinancialReceivable>.Failed(
                "ProviderResponseInvalid",
                CreateFinancialProviderDiagnostic("GetReceivable", "GET", "/schedules/credit/{scheduleId}", httpStatusCode, exception));
        }
    }

    public async Task<FinancialQueryResult<FinancialReceivable>> FindReceivablesByReferenceAsync(
        FinancialReceivableReferenceQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var chargeReference = query.ChargeId.ToString();
        var catalogQuery = new FinancialCatalogQuery(
            query.TenantId,
            query.Provider,
            query.ConnectionId,
            Take: query.PageSize);
        var matchingReceivables = new List<FinancialReceivable>();
        var seenReceivableIds = new HashSet<string>(StringComparer.Ordinal);
        int? expectedTotalCount = null;

        // Percorre o catálogo e rejeita páginas inconsistentes ou recebíveis repetidos.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var receivablePage = await ReadCatalogAsync<NiboReceivableResponse, FinancialReceivable>(
                catalogQuery,
                connection,
                (niboApi, accessToken) => niboApi.FindReceivablesAsync(
                    $"reference eq '{chargeReference}'",
                    catalogQuery.Skip,
                    catalogQuery.Take,
                    "dueDate,scheduleId",
                    accessToken,
                    cancellationToken),
                receivableResponse =>
                {
                    var receivable = MapReceivableResponse(receivableResponse);

                    if (receivable.Reference != chargeReference)
                    {
                        throw new JsonException();
                    }

                    return receivable;
                },
                cancellationToken);

            if (!receivablePage.Success)
            {
                return FinancialQueryResult<FinancialReceivable>.Failed(receivablePage.ErrorCode!);
            }

            if (expectedTotalCount is not null &&
                expectedTotalCount != receivablePage.TotalCount)
            {
                return FinancialQueryResult<FinancialReceivable>.Failed("ProviderResponseInconsistent");
            }

            expectedTotalCount = receivablePage.TotalCount;

            foreach (var receivable in receivablePage.Items)
            {
                if (!seenReceivableIds.Add(receivable.ProviderExternalId))
                {
                    return FinancialQueryResult<FinancialReceivable>.Failed("ProviderResponseInconsistent");
                }

                // Apenas recebíveis ativos participam da decisão de reutilização.
                if (receivable.IsActive)
                {
                    matchingReceivables.Add(receivable);
                }
            }

            catalogQuery = catalogQuery with
            {
                Skip = catalogQuery.Skip + receivablePage.Items.Count
            };

            if (catalogQuery.Skip >= receivablePage.TotalCount)
            {
                return FinancialQueryResult<FinancialReceivable>.Completed(
                    matchingReceivables.ToArray(),
                    matchingReceivables.Count);
            }
        }
    }

    private static FinancialReceivable MapReceivableResponse(NiboReceivableResponse? receivableResponse)
    {
        if (receivableResponse is null ||
            !Guid.TryParse(receivableResponse.ScheduleId, out var scheduleId) ||
            scheduleId == Guid.Empty ||
            receivableResponse.Type != "Credit" ||
            receivableResponse.Value is null ||
            receivableResponse.ScheduleDate is null ||
            receivableResponse.DueDate is null ||
            receivableResponse.AccrualDate is null ||
            receivableResponse.IsPaid is null ||
            receivableResponse.Categories is null ||
            receivableResponse.Categories.Count == 0 ||
            receivableResponse.Categories.Any(category =>
                category is null ||
                string.IsNullOrWhiteSpace(category.CategoryId) ||
                category.Value is null))
        {
            throw new JsonException();
        }

        // A Nibo pode informar o cliente no objeto aninhado ou no campo principal.
        var nestedCustomerId = receivableResponse.Stakeholder?.Id;

        if (Guid.TryParse(nestedCustomerId, out var parsedNestedCustomerId))
        {
            nestedCustomerId = parsedNestedCustomerId == Guid.Empty ? null : parsedNestedCustomerId.ToString();
        }

        var topLevelCustomerId = receivableResponse.StakeholderId;

        if (Guid.TryParse(topLevelCustomerId, out var parsedTopLevelCustomerId))
        {
            topLevelCustomerId = parsedTopLevelCustomerId == Guid.Empty ? null : parsedTopLevelCustomerId.ToString();
        }

        var customerId = string.IsNullOrWhiteSpace(nestedCustomerId) ? topLevelCustomerId : nestedCustomerId;

        if (string.IsNullOrWhiteSpace(customerId) ||
            customerId == Guid.Empty.ToString() ||
            (!string.IsNullOrWhiteSpace(topLevelCustomerId) &&
                !string.IsNullOrWhiteSpace(nestedCustomerId) &&
                topLevelCustomerId != nestedCustomerId))
        {
            throw new JsonException();
        }

        return new(
            scheduleId.ToString(),
            receivableResponse.Reference,
            customerId,
            receivableResponse.Value.Value,
            DateOnly.FromDateTime(receivableResponse.ScheduleDate.Value.DateTime),
            DateOnly.FromDateTime(receivableResponse.DueDate.Value.DateTime),
            DateOnly.FromDateTime(receivableResponse.AccrualDate.Value.DateTime),
            receivableResponse.Description,
            receivableResponse.Categories.Select(category => new FinancialReceivableCategory(
                category.CategoryId!,
                category.Value!.Value)).ToArray(),
            receivableResponse.IsPaid.Value,
            receivableResponse.DeleteDate is null &&
                receivableResponse.IsDeleted != true &&
                receivableResponse.IsArchived != true &&
                receivableResponse.Stakeholder?.IsDeleted != true &&
                receivableResponse.Stakeholder?.IsArchived != true);
    }

    private static string MapHttpStatusToErrorCode(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Unauthorized",
        HttpStatusCode.TooManyRequests => "RateLimited",
        HttpStatusCode.NotFound => "ProviderNotFound",
        _ when (int)status >= 500 ||
            (int)status is >= 300 and < 400 ||
            status == HttpStatusCode.RequestTimeout => "ProviderUnavailable",
        _ => "ProviderRejected"
    };
}
