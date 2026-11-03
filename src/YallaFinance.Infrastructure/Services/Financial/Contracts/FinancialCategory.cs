namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialCategory(string ProviderExternalId, string Name, FinancialCategoryKind Kind);
