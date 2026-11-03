using YallaFinance.Application;
namespace YallaFinance.Application.Services.Charges.Contracts;

public sealed record ChargeSettlementTarget(Guid ConnectionId, DateOnly ReceiptDate);
