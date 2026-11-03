using Refit;
using YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;
namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal interface INiboApi
{
    [Post("/schedules/credit/{scheduleId}/receipts")]
    Task<HttpResponseMessage> SettleReceivableAsync(
        Guid scheduleId,
        [Body] SettleNiboReceivableRequest request,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/receipts")]
    Task<HttpResponseMessage> FindReceiptsAsync(
        [AliasAs("$filter")] string filter,
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/customers/{customerId}")]
    Task<HttpResponseMessage> GetCustomerAsync(
        Guid customerId,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/schedules/credit/{scheduleId}")]
    Task<HttpResponseMessage> GetReceivableAsync(
        Guid scheduleId,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/schedules/credit")]
    Task<HttpResponseMessage> FindReceivablesAsync(
        [AliasAs("$filter")] string filter,
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Post("/customers/FormatType=json")]
    Task<HttpResponseMessage> CreateCustomerAsync(
        [Body] CreateNiboCustomerRequest request,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Post("/schedules/credit/FormatType=json")]
    Task<HttpResponseMessage> CreateReceivableAsync(
        [Body] CreateNiboReceivableRequest request,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/customers")]
    Task<HttpResponseMessage> FindCustomersAsync(
        [AliasAs("$filter")] string filter,
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/customers")]
    Task<HttpResponseMessage> ListCustomersAsync(
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/costcenters")]
    Task<HttpResponseMessage> ListCostCentersAsync(
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
    [Get("/categories")]
    Task<HttpResponseMessage> ListCategoriesAsync(
        [AliasAs("$skip")] int skip,
        [AliasAs("$top")] int take,
        [AliasAs("$orderby")] string orderBy,
        [Header("ApiToken")] string apiToken,
        CancellationToken cancellationToken);
}
