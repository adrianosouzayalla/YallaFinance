using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record SettleReceivableExpectation(
    string Identifier,
    Guid AccountId,
    decimal Amount,
    DateOnly Date);
