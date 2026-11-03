using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

/// <summary>Bank evidence at ObservedAt; this is neither financial settlement nor authorization to issue again.</summary>
public sealed record PixChargeSnapshot(
    string ProviderReference,
    decimal Amount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    PixChargeStatus Status,
    string? CopyPaste,
    DateTimeOffset ObservedAt,
    PixChargePayer? Payer = null,
    string? Description = null,
    string? Location = null,
    string? LocationId = null)
{
    public bool IsPayable => Status == PixChargeStatus.Active &&
        ExpiresAt > ObservedAt &&
        CopyPaste is not null;
    public override string ToString() => $"PixChargeSnapshot {{ Status = {Status}, Instrument = [redacted] }}";
}
