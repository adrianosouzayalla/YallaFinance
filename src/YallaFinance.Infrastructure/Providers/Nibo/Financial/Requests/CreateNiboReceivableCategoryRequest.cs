namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;

internal sealed record CreateNiboReceivableCategoryRequest(
    string CategoryId,
    decimal Value,
    string Description);
