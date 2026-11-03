using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixReceivedParametersResponse(
    [property: JsonPropertyName("paginacao")] SicoobPixReceivedPaginationResponse? Pagination);
