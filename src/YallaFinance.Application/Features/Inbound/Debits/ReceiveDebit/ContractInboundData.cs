using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ContractInboundData(
    string ExternalId,
    string Number,
    string? Plate,
    string? Product,
    string? Fleet);
