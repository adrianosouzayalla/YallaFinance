using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboCostCenterResponse(
    [property: JsonPropertyName("costCenterId")] string? Id,
    string? Description);
