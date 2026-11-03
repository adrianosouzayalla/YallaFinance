namespace YallaFinance.Domain.Payments;

public sealed record PaymentEligibility(
    decimal Amount,
    decimal ItemsAmount,
    int ContractCount,
    int ItemCount,
    bool ValidDebts,
    bool HasOpenRisk,
    bool HasActiveRequest)
{
    public string? BlockReason() => HasOpenRisk ? "OpenFinancialRisk"
        : HasActiveRequest ? "ActiveRequestExists"
            : ItemCount == 0 ? "MissingItems"
                : ContractCount != 1 ? "MultipleContracts"
                    : Amount <= 0 || Amount != ItemsAmount || decimal.Round(Amount, 2) != Amount ? "InconsistentAmount"
                        : !ValidDebts ? "IneligibleDebts" : null;
}
