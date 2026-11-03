using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Charges.Contracts.Settlement;

public sealed record ChargeSettlementItemCheckpoint(
    SettleReceivableData Data,
    string State = "NotSent",
    string? ProviderReceiptId = null,
    string? ErrorCode = null,
    FinancialProviderDiagnostic? Diagnostic = null);
