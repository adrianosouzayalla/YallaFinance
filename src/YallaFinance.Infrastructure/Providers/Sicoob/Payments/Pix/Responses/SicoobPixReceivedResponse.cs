using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixReceivedResponse(
    [property: JsonPropertyName("pix")] IReadOnlyList<SicoobPixReceivedItemResponse>? Pix,
    [property: JsonPropertyName("parametros")] SicoobPixReceivedParametersResponse? Parameters);
