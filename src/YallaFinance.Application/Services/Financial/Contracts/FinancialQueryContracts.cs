namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record FinancialCustomerLookup(Guid TenantId, string Document);

public sealed record FinancialCatalogPage(Guid TenantId, int Skip = 0, int Take = 100);

public sealed record FinancialCustomerSummary(string ExternalReference, string Name, string? Document,
    string? Email = null, string? Phone = null,
    YallaFinance.Domain.Customers.CustomerAddressData? Address = null)
{
    public string? DocumentType => YallaFinance.Domain.Customers.CustomerDocument.HasValidFormat(Document, "CPF") ? "CPF"
        : YallaFinance.Domain.Customers.CustomerDocument.HasValidFormat(Document, "CNPJ") ? "CNPJ" : null;
}

public sealed record FinancialCostCenterSummary(string ExternalReference, string Name);

public enum FinancialCategoryType
{
    Income,
    Expense
}

public sealed record FinancialCategorySummary(string ExternalReference, string Name, FinancialCategoryType Kind);

public enum FinancialCatalogError
{
    Validation,
    ConfigurationUnavailable,
    OperationNotSupported,
    ProviderUnavailable,
    InvalidResponse,
    ProviderUnauthorized,
    ProviderNotFound,
    RateLimited,
    ProviderRejected
}

public sealed record FinancialCatalogResult<T>(
    bool Success,
    IReadOnlyList<T> Items,
    int TotalCount,
    Guid? ConnectionId,
    FinancialCatalogError? Error)
{
    public static FinancialCatalogResult<T> Completed(Guid connectionId, IReadOnlyList<T> items, int totalCount)
        => new(true, items, totalCount, connectionId, null);

    public static FinancialCatalogResult<T> Failed(FinancialCatalogError error)
        => new(false, [], 0, null, error);
}

public sealed record FinancialCatalogPageResponse<T>(Guid ConnectionId, IReadOnlyList<T> Items, int TotalCount);
