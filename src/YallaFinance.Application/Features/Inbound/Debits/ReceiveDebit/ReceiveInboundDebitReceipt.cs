using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ReceiveInboundDebitReceipt(Guid OperationId, bool Created);
