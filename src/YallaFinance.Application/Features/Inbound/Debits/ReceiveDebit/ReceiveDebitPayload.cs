using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ReceiveDebitPayload(DebitInboundData Debit, CustomerInboundData Customer);
