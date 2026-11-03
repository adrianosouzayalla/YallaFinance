using YallaFinance.Application.Services.Payments.Pix.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record MatchedPaymentReceipt(
    Guid TenantId,
    Guid ChargeId,
    Guid PaymentAttemptId,
    Guid BankConnectionId,
    PixReceivedTransaction Receipt);
