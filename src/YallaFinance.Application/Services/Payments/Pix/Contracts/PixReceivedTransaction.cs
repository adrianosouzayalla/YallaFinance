using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixReceivedTransaction(
    string EndToEndId,
    string? TxId,
    decimal Amount,
    DateTimeOffset ReceivedAt,
    string ProviderPayloadJson);
