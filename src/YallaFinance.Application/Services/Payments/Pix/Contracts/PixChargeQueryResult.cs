using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixChargeQueryResult(
    PixChargeQueryOutcome Outcome,
    Guid ConnectionId,
    string Provider,
    string ProviderReference,
    PixChargeSnapshot? Charge = null,
    string? ErrorCode = null)
{
    public string? FailureMessage { get; init; }

    public override string ToString() => nameof(PixChargeQueryResult);
}
