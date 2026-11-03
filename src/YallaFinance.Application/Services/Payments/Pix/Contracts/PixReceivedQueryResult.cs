using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixReceivedQueryResult(
    PixReceivedQueryOutcome Outcome,
    Guid ConnectionId,
    string Provider,
    IReadOnlyList<PixReceivedTransaction> Transactions,
    PixReceivedPagination? Pagination = null,
    string? ErrorCode = null,
    DateTimeOffset? Start = null,
    DateTimeOffset? End = null);
