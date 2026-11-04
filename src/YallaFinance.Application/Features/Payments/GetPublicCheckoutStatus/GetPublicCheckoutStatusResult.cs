using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Services.Payments.Contracts;

namespace YallaFinance.Application.Features.Payments.GetPublicCheckoutStatus;

public sealed record GetPublicCheckoutStatusResult(NotificationResult<PublicPaymentCheckoutStatus> Response);

/// <summary>
/// The fields the public checkout needs to decide whether its loaded data is still current.
/// Methods, contract and card requirements are left out; a change in them is loaded in full.
/// </summary>
public sealed record PublicPaymentCheckoutStatus(
    string Status,
    DateTime ExpiresAt,
    bool HasReceivedPayment,
    decimal ReceivedAmount,
    DateTime? ReceivedAt,
    bool Settled,
    DateTime? SettledAt,
    PublicPaymentAttemptResult? Attempt)
{
    public static PublicPaymentCheckoutStatus From(PublicPaymentCheckout checkout) => new(
        checkout.Status,
        checkout.ExpiresAt,
        checkout.HasReceivedPayment,
        checkout.ReceivedAmount,
        checkout.ReceivedAt,
        checkout.Settled,
        checkout.SettledAt,
        checkout.Attempt);
}
