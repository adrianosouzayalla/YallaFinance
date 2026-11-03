using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;

internal sealed record SicoobCreatePixChargeRequest(
    [property: JsonPropertyName("calendario")] SicoobPixCalendarRequest Calendar,
    [property: JsonPropertyName("valor")] SicoobPixAmountRequest Amount,
    [property: JsonPropertyName("chave")] string ReceiverKey,
    [property: JsonPropertyName("devedor")] SicoobPixPayerRequest? Payer,
    [property: JsonPropertyName("solicitacaoPagador")] string? Description);
