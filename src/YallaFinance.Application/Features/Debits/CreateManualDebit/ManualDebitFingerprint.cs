using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Debits.CreateManualDebit;

public static class ManualDebitFingerprint
{
    public static string Compute(CreateManualDebitCommand input) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            input.CustomerId,
            input.ContractId,
            input.CategoryId,
            Amount = input.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            input.OccurrenceDate,
            input.DueDate,
            input.Description
        }, JsonSerializerOptions.Web)));
}
