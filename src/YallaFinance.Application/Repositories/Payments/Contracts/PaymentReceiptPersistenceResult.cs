using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentReceiptPersistenceResult(
    int WithTxId,
    int Matched,
    int AlreadyStored,
    int Unmatched,
    int Stored);
