using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record DebitInboundData(
    string ExternalId,
    decimal Amount,
    DateOnly OccurrenceDate,
    DateOnly? DueDate,
    string Description,
    string CategoryCode,
    ContractInboundData Contract);
