using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record SettleReceivableData(
    Guid TenantId,
    Guid ConnectionId,
    string ExpectedProvider,
    Guid ChargeItemId,
    Guid PaymentTransactionId,
    string ProviderExternalId,
    Guid ProviderAccountId,
    decimal Amount,
    DateOnly ReceiptDate,
    string Identifier,
    bool PreviousAttemptUncertain = false,
    IReadOnlyList<SettleReceivableExpectation>? ExpectedReceipts = null);
