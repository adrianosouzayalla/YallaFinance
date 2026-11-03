using YallaFinance.Infrastructure.Providers.Financial;
using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
using System.Net;
using System.Text.Json;
using Refit;
using Microsoft.Extensions.Logging;

using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Services.Financial;
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
using YallaFinance.Application.Repositories.Connections;
namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider(
    NiboHttpClientFactory clients,
    NiboConfigurationReader configuration,
    ICompanyConnectionRepository connections,
    FinancialOperationCoordinator coordinator,
    ILogger<NiboFinancialProvider> logger) : IFinancialProvider, IFinancialReceivableSettlementProvider
{
    public string Provider => "Nibo";

    public async Task<CreateReceivableResult> CreateReceivableAsync(
        CreateReceivableIntegrationData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var options = await configuration.ReadAsync(connection, cancellationToken);
        var customer = data.ProviderCustomerId.ToString();
        var category = await connections.ResolveMappingAsync(
            data.TenantId,
            connection.Id,
            "Category",
            data.CategoryId.ToString(),
            cancellationToken);
        if (category is null)
            return CreateReceivableResult.Rejected("MappingMissing");
        if (!Guid.TryParse(category, out var categoryId) || categoryId == Guid.Empty)
            return CreateReceivableResult.Rejected("InvalidMapping");
        return await SendReceivableAsync(data, customer, category, options, cancellationToken);
    }

    private async Task<CreateReceivableResult> SendReceivableAsync(
        CreateReceivableIntegrationData data,
        string customer,
        string category,
        NiboOptions options,
        CancellationToken cancellationToken)
    {
        var request = new CreateNiboReceivableRequest(
            customer,
            data.Description,
            data.ChargeId.ToString(),
            data.ScheduleDate,
            data.DueDate,
            data.AccrualDate,
            [new CreateNiboReceivableCategoryRequest(category, data.Amount, data.Description)],
            CostCenterValueType: string.IsNullOrWhiteSpace(data.ExternalCostCenterId) ? null : 1,
            CostCenters: string.IsNullOrWhiteSpace(data.ExternalCostCenterId)
                ? null : [new(data.ExternalCostCenterId, 100)]);
        using var client = clients.Create(options);
        var niboApi = RestService.For<INiboApi>(
            client,
            new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)) });
        await coordinator.AcquireHttpSlotAsync(options.ApiToken, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var response = await niboApi.CreateReceivableAsync(
                request,
                options.ApiToken,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var httpStatusCode = (int)response.StatusCode;
                var providerError = await ReadProviderErrorAsync(response.Content, cancellationToken);
                var diagnostic = CreateFinancialProviderDiagnostic("CreateReceivable", "POST", "/schedules/credit", httpStatusCode) with
                {
                    ProviderErrorCode = providerError.Code,
                    ProviderErrorMessage = providerError.Message
                };
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    coordinator.DelayRequests(
                        options.ApiToken,
                        response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(1));
                // Nibo reports this explicit pre-creation rejection using HTTP 500.
                // Other server errors remain uncertain and must be reconciled.
                if (httpStatusCode == 500 && providerError.Code == "validation_error"
                    && providerError.Message == "Categoria não encontrada")
                    return CreateReceivableResult.Rejected("ProviderRejected", diagnostic);
                if (httpStatusCode >= 500 ||
                    httpStatusCode is >= 300 and < 400 ||
                    response.StatusCode == HttpStatusCode.RequestTimeout)
                    return CreateReceivableResult.Indeterminate(diagnostic);
                return CreateReceivableResult.Rejected(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Unauthorized",
                    HttpStatusCode.TooManyRequests => "RateLimited",
                    _ => "ProviderRejected"
                }, diagnostic);
            }
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException)
            {
                return CreateReceivableResult.Indeterminate();
            }
            var result = JsonSerializer.Deserialize<CreateNiboReceivableResponse>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return string.IsNullOrWhiteSpace(result?.ScheduleId) ? CreateReceivableResult.Indeterminate() : CreateReceivableResult.Completed(result.ScheduleId);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or ApiException or ApiRequestException)
        {
            return CreateReceivableResult.Indeterminate();
        }
    }
}
