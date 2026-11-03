using YallaFinance.Application;
namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public sealed record ChargeIntegrationSummary(
    Guid ChargeId,
    string Outcome,
    int TotalItems,
    int ConfirmedItems,
    int RejectedItems,
    int IndeterminateItems,
    IReadOnlyList<ChargeIntegrationItemResult> Items);

