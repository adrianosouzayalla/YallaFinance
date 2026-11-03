using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public sealed record ReceivableSettlementQueryResult(
    ReceivableSettlementState State,
    string? ProviderReceiptId = null,
    string? ErrorCode = null,
    FinancialProviderDiagnostic? Diagnostic = null);
