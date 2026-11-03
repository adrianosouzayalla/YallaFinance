namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboListResponse<T>(IReadOnlyList<T>? Items, int? Count);
