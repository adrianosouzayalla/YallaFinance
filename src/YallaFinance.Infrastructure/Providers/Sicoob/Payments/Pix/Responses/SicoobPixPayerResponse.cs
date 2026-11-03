using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;

internal sealed record SicoobPixPayerResponse(
    [property: JsonPropertyName("cpf")] string? Cpf,
    [property: JsonPropertyName("cnpj")] string? Cnpj,
    [property: JsonPropertyName("nome")] string? Name);
