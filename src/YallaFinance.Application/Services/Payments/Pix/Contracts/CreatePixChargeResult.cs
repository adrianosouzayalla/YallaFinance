using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record CreatePixChargeResult(
    PixChargeCreationOutcome Outcome,
    Guid ConnectionId,
    string Provider,
    string ProviderReference,
    PixChargeSnapshot? Charge = null,
    string? ErrorCode = null,
    bool AlreadyExisted = false)
{
    public bool Success => Outcome == PixChargeCreationOutcome.Completed;
    public string? FailureMessage { get; init; }

    public override string ToString() => nameof(CreatePixChargeResult);
}
