using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Infrastructure.Providers;

using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Infrastructure.Services.Financial.Contracts;
namespace YallaFinance.Infrastructure.Providers.Financial;

internal interface IFinancialProvider : IProviderCapability
{
    Task<CreateChargeResult> CreateChargeAsync(
        CreateChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken);

    Task<FinancialQueryResult<FinancialReceivable>> GetReceivableAsync(
        FinancialReceivableIdQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialReceivable>.Failed("OperationNotSupported"));

    Task<FinancialQueryResult<FinancialReceivable>> FindReceivablesByReferenceAsync(
        FinancialReceivableReferenceQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialReceivable>.Failed("OperationNotSupported"));

    Task<CreateCustomerResult> CreateCustomerAsync(
        CreateCustomerIntegrationData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateCustomerResult.Rejected("OperationNotSupported"));

    Task<CreateReceivableResult> CreateReceivableAsync(
        CreateReceivableIntegrationData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(CreateReceivableResult.Rejected("OperationNotSupported"));

    Task<FinancialQueryResult<FinancialCustomer>> ListCustomersAsync(
        FinancialCatalogQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialCustomer>.Failed("OperationNotSupported"));

    Task<FinancialQueryResult<FinancialCustomer>> FindCustomersByDocumentAsync(
        FinancialCustomerLookupData data, ProviderConnection connection, CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialCustomer>.Failed("OperationNotSupported"));

    Task<FinancialQueryResult<FinancialCostCenter>> ListCostCentersAsync(
        FinancialCatalogQuery query, ProviderConnection connection, CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialCostCenter>.Failed("OperationNotSupported"));

    Task<FinancialQueryResult<FinancialCategory>> ListCategoriesAsync(
        FinancialCatalogQuery query, ProviderConnection connection, CancellationToken cancellationToken)
        => Task.FromResult(FinancialQueryResult<FinancialCategory>.Failed("OperationNotSupported"));
}
