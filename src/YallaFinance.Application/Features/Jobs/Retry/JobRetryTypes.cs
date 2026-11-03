namespace YallaFinance.Application.Features.Jobs.Retry;

// Tipos de Job que aceitam retry automatico, canonicos no codigo (nao vem de entrada do cliente).
public static class JobRetryTypes
{
    public const int MaximumRetries = 20;

    public static readonly IReadOnlyList<string> All =
    [
        "FINANCIAL_CHARGE_CREATE_V1",
        "FINANCIAL_CHARGE_SETTLE_V1",
        "FINANCIAL_DEBIT_INBOUND_V1"
    ];
}
