using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Payments;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public static class ChargeCommandFingerprint
{
    public static string Hash(Guid tenant, CreateChargeCommand request) => Digest(
        request.Source == ChargeSource.Selected
            ? (object)new
            {
                Version = 2,
                Tenant = tenant,
                Customer = Guid.Parse(request.CustomerId),
                request.Source,
                Debts = request.DebtIds.Select(Guid.Parse).Order().ToArray(),
                Amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                request.DueDate,
                Note = request.CustomerNote ?? ""
            }
            : new
            {
                Version = 2,
                Tenant = tenant,
                Customer = Guid.Parse(request.CustomerId),
                request.Source,
                Amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                request.OccurrenceDate,
                request.DescriptionDetail,
                Movement = Guid.Parse(request.SelectedMovementId!),
                Category = Guid.Parse(request.CategoryId),
                request.DueDate,
                Note = request.CustomerNote ?? ""
            });

    // The original command can still recover a persisted charge after the legacy
    // column is dropped. Compare its original hash; never rewrite existing hashes.
    public static string? LegacyHash(Guid tenant, CreateChargeCommand request)
    {
        if (!Guid.TryParse(request.PaymentMethodId, out var paymentMethodId) || !Enum.IsDefined(request.PaymentMethod))
            return null;
        return Digest(request.Source == ChargeSource.Selected
            ? (object)new
            {
                Tenant = tenant,
                Customer = Guid.Parse(request.CustomerId),
                request.Source,
                Debts = request.DebtIds.Select(Guid.Parse).Order().ToArray(),
                Method = paymentMethodId,
                Amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                request.DueDate,
                Note = request.CustomerNote ?? ""
            }
            : new
            {
                Tenant = tenant,
                Customer = Guid.Parse(request.CustomerId),
                request.Source,
                Method = paymentMethodId,
                Amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                request.OccurrenceDate,
                request.DescriptionDetail,
                Movement = Guid.Parse(request.SelectedMovementId!),
                Category = Guid.Parse(request.CategoryId),
                request.PaymentMethod,
                request.DueDate,
                Note = request.CustomerNote ?? ""
            });
    }

    private static string Digest(object value) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
}
