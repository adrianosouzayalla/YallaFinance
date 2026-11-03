using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixReceivedQuery(
    Guid TenantId,
    Guid ConnectionId,
    string ExpectedProvider,
    DateTimeOffset? Start = null,
    DateTimeOffset? End = null,
    string? TxId = null,
    int Page = 0,
    int ItemsPerPage = 100);
