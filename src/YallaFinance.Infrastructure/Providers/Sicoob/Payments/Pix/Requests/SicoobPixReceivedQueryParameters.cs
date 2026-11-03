using Refit;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;

internal sealed record SicoobPixReceivedQueryParameters(
    [property: AliasAs("inicio")] string Start,
    [property: AliasAs("fim")] string End,
    [property: AliasAs("txid")] string? TxId,
    [property: AliasAs("paginacao.paginaAtual")] int Page,
    [property: AliasAs("paginacao.itensPorPagina")] int ItemsPerPage);
