using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixReceivedItemResponse(
    [property: JsonPropertyName("endToEndId")] string? EndToEndId,
    [property: JsonPropertyName("txid")] string? TxId,
    [property: JsonPropertyName("valor")] string? Amount,
    [property: JsonPropertyName("horario")] string? ReceivedAt);
