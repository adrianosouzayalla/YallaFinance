using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;
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
    public async Task<CreateCustomerResult> CreateCustomerAsync(
        CreateCustomerIntegrationData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var options = await configuration.ReadAsync(connection, cancellationToken);
        CreateNiboCustomerAddressRequest? address;
        try
        {
            address = CreateNiboCustomerAddressRequest.FromCustomerAddress(data.Address);
        }
        catch (ArgumentException)
        {
            return CreateCustomerResult.Rejected("CustomerAddressNotSupported");
        }
        var request = new CreateNiboCustomerRequest(
            data.Name,
            data.Document is null ? null : new(data.Document, data.Document.Length == 11 ? "CPF" : "CNPJ"),
            data.Email is null && data.Phone is null ? null : new(data.Email, data.Phone),
            address);
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        using var client = clients.Create(options);
        var niboApi = RestService.For<INiboApi>(
            client,
            new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions) });
        await coordinator.AcquireHttpSlotAsync(options.ApiToken, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var response = await niboApi.CreateCustomerAsync(
                request,
                options.ApiToken,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var httpStatusCode = (int)response.StatusCode;
                var providerError = await ReadProviderErrorAsync(response.Content, cancellationToken);
                var diagnostic = CreateFinancialProviderDiagnostic("CreateCustomer", "POST", "/customers", httpStatusCode) with
                {
                    ProviderErrorCode = providerError.Code,
                    ProviderErrorMessage = providerError.Message
                };
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    coordinator.DelayRequests(
                        options.ApiToken,
                        response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(1));
                if (httpStatusCode >= 500 ||
                    httpStatusCode is >= 300 and < 400 ||
                    response.StatusCode == HttpStatusCode.RequestTimeout)
                    return CreateCustomerResult.Indeterminate(diagnostic);
                return CreateCustomerResult.Rejected(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Unauthorized",
                    HttpStatusCode.TooManyRequests => "RateLimited",
                    _ => "ProviderRejected"
                }, diagnostic);
            }
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CreateNiboCustomerResponse>(body, serializerOptions);
            return string.IsNullOrWhiteSpace(result?.Id) ? CreateCustomerResult.Indeterminate() : CreateCustomerResult.Completed(result.Id);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or ApiException or ApiRequestException or InvalidOperationException or IOException)
        {
            return CreateCustomerResult.Indeterminate();
        }
    }
}
