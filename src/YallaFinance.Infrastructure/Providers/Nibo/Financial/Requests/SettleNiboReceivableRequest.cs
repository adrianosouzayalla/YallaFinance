namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;

internal sealed record SettleNiboReceivableRequest(
    Guid AccountId,
    DateOnly Date,
    string Identifier,
    decimal Value);
