using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixReceivedPaginationResponse(
    [property: JsonPropertyName("paginaAtual")] int? Page,
    [property: JsonPropertyName("itensPorPagina")] int? ItemsPerPage,
    [property: JsonPropertyName("quantidadeDePaginas")] int? TotalPages,
    [property: JsonPropertyName("quantidadeTotalDeItens")] int? TotalItems);
