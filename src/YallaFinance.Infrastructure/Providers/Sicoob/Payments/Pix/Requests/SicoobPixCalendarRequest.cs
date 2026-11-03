using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;

internal sealed record SicoobPixCalendarRequest([property: JsonPropertyName("expiracao")] int ExpirationSeconds);
