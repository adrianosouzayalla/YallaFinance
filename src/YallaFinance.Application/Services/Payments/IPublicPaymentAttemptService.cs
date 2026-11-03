using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Services.Payments;

public interface IPublicPaymentAttemptService
{
    Task<NotificationResult<PublicPaymentAttemptResult>> StartOrGetAsync(
        string token,
        string methodCode,
        string idempotencyKey,
        CancellationToken cancellationToken,
        PublicCardPaymentInput? card = null);
}
