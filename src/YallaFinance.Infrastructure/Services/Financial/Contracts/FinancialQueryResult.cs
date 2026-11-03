using YallaFinance.Application.Services.Financial.Contracts;


namespace YallaFinance.Infrastructure.Services.Financial.Contracts;

internal sealed record FinancialQueryResult<T>(
    bool Success,
    IReadOnlyList<T> Items,
    int TotalCount,
    string? ErrorCode,
    FinancialProviderDiagnostic? Diagnostic = null)
{
    public static FinancialQueryResult<T> Completed(IReadOnlyList<T> items, int totalCount) => new(
        true,
        items,
        totalCount,
        null);
    public static FinancialQueryResult<T> Failed(string code, FinancialProviderDiagnostic? diagnostic = null)
        => new(false, Array.Empty<T>(), 0, code, diagnostic);
}
