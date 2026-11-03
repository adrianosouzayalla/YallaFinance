using System.Security.Cryptography;
using System.Text.Json;
using YallaFinance.Application.Repositories.Payments.Contracts;


namespace YallaFinance.Infrastructure.Repositories.Payments;

public static class PaymentCommandFingerprint
{
    public static string Hash(PublishPaymentRequestData data) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            data.ChargeId,
            Methods = data.PaymentMethodIds.Order().ToArray(),
            Expiration = data.ExpiresAt.UtcDateTime,
            data.Note
        })));
}
