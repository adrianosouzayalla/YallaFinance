using System.Text.RegularExpressions;
using YallaFinance.Application.Services.Payments.Pix.Contracts;


namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal static partial class SicoobPixValidation
{
    public static bool IsValidProviderReference(string? reference) => reference is not null &&
        TxIdPattern().IsMatch(reference);

    public static bool IsValidPixChargeData(CreatePixChargeData data) => IsValidProviderReference(data.ProviderReference)
        && data.Amount > 0 && data.Amount <= 9999999999.99m && decimal.Round(data.Amount, 2) == data.Amount
        && data.ExpirationSeconds > 0 && (data.Description is null || data.Description.Length <= 140 && !data.Description.Any(char.IsControl))
        && (data.Payer is null || IsValidPixPayer(data.Payer));

    internal static bool IsValidPixPayer(PixChargePayer payer) => payer.TaxId is not null && payer.TaxId.Length is 11 or 14
        && payer.TaxId.All(char.IsAsciiDigit) && !string.IsNullOrWhiteSpace(payer.Name)
        && payer.Name.Length <= 200 && !payer.Name.Any(char.IsControl);

    [GeneratedRegex(@"^[a-zA-Z0-9]{26,35}\z", RegexOptions.CultureInvariant)]
    private static partial Regex TxIdPattern();
}
