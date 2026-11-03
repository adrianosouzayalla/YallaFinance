using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixCalendarResponse(
    [property: JsonPropertyName("criacao")] string? CreatedAt,
    [property: JsonPropertyName("expiracao")] int? ExpirationSeconds);
