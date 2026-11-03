using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixAmountResponse([property: JsonPropertyName("original")] string? Original);
