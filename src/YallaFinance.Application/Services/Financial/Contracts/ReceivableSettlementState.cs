using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

public enum ReceivableSettlementState
{
    Open,
    MatchingReceipt,
    ConflictingReceipt,
    Rejected,
    Indeterminate
}
