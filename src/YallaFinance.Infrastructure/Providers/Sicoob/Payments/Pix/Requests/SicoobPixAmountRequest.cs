using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;

internal sealed record SicoobPixAmountRequest([property: JsonPropertyName("original")] string Original);
