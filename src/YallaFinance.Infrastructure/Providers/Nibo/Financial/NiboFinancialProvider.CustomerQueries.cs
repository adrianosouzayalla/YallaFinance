using YallaFinance.Infrastructure.Providers.Configuration;
using System.Text.Json;
using Refit;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using YallaFinance.Infrastructure.Services.Financial;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    private async Task<FinancialQueryResult<FinancialCustomer>> GetCustomerAsync(
        string id,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var options = await configuration.ReadAsync(connection, cancellationToken);
        using var client = clients.Create(options);
        var niboApi = RestService.For<INiboApi>(client);
        try
        {
            using var response = await SendReadAsync(
                () => niboApi.GetCustomerAsync(
                    Guid.Parse(id),
                    options.ApiToken,
                    cancellationToken),
                options.ApiToken,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return FinancialQueryResult<FinancialCustomer>.Failed(MapHttpStatusToErrorCode(response.StatusCode));
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var item = JsonSerializer.Deserialize<NiboCustomerResponse>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            // A Nibo pode retornar HTTP 200 com corpo nulo para um cliente removido.
            if (item is null)
                return FinancialQueryResult<FinancialCustomer>.Failed("ProviderRemoved");
            if (item.Id != id ||
                string.IsNullOrWhiteSpace(item.Name) ||
                item.IsDeleted is null ||
                item.IsArchived is null)
                return FinancialQueryResult<FinancialCustomer>.Failed("ProviderResponseInvalid");
            return FinancialQueryResult<FinancialCustomer>.Completed(
                [new(
                    id,
                    item.Name,
                    FinancialDocument.Normalize(item.Document?.Number),
                    item.IsDeleted.Value,
                    item.IsArchived.Value,
                    item.ContactEmail,
                    item.ContactPhone,
                    item.IsDeleted.Value || item.IsArchived.Value ? null : item.Address?.ToCustomerAddress())],
                1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or ApiException or ApiRequestException)
        {
            return FinancialQueryResult<FinancialCustomer>.Failed("ProviderUnavailable");
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException)
        {
            return FinancialQueryResult<FinancialCustomer>.Failed("ProviderResponseInvalid");
        }
    }
}
