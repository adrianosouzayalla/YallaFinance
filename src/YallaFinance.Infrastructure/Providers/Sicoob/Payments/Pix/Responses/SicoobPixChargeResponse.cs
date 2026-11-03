using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixChargeResponse(
    [property: JsonPropertyName("txid")] string? TxId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("calendario")] SicoobPixCalendarResponse? Calendar,
    [property: JsonPropertyName("valor")] SicoobPixAmountResponse? Amount,
    [property: JsonPropertyName("chave")] string? ReceiverKey,
    [property: JsonPropertyName("pixCopiaECola")] string? CopyPaste,
    [property: JsonPropertyName("brcode")] string? Brcode,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("loc")] SicoobPixLocationResponse? LocationData,
    [property: JsonPropertyName("devedor")] SicoobPixPayerResponse? Payer,
    [property: JsonPropertyName("solicitacaoPagador")] string? Description);
