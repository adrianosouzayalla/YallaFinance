using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;

namespace YallaFinance.Application.Services.Financial;

public interface IFinancialIntegrationService
{
    Task<CreateChargeResult> CreateChargeAsync(CreateChargeData data, CancellationToken cancellationToken);
    Task<FinancialCatalogResult<FinancialCustomerSummary>> FindCustomersByDocumentAsync(
        FinancialCustomerLookup query, CancellationToken cancellationToken);
    Task<FinancialCatalogResult<FinancialCostCenterSummary>> ListCostCentersAsync(
        FinancialCatalogPage query, CancellationToken cancellationToken);
    Task<FinancialCatalogResult<FinancialCategorySummary>> ListCategoriesAsync(
        FinancialCatalogPage query, CancellationToken cancellationToken);
}
