using System.Text.Json;
using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixLocationResponse(
    [property: JsonPropertyName("id")] JsonElement? Id,
    [property: JsonPropertyName("location")] string? Location);
