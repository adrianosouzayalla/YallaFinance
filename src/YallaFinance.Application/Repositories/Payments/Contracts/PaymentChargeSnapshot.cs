using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentChargeSnapshot(
    Guid Id,
    string CustomerName,
    string Contract,
    decimal Amount,
    DateTime CreatedAt,
    DateOnly DueDate,
    bool Eligible,
    string? BlockReason,
    ActivePaymentRequestSummary? ActivePaymentRequest = null,
    PaymentActivitySummary? Activity = null,
    decimal ReceivedAmount = 0,
    bool Settled = false,
    DateTime? ReceivedAt = null,
    DateTime? SettledAt = null,
    IReadOnlyList<PaymentReceiptSummary>? Receipts = null,
    string Reference = "");
