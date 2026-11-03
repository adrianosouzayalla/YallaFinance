using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PublicPaymentCheckout(
    string Status,
    DateTime ExpiresAt,
    decimal Amount,
    DateOnly DueDate,
    string ContractNumber,
    IReadOnlyList<PublicPaymentMethodOption> Methods,
    PublicPaymentAttemptResult? Attempt = null,
    bool HasReceivedPayment = false,
    decimal ReceivedAmount = 0,
    DateTime? ReceivedAt = null,
    bool Settled = false,
    DateTime? SettledAt = null,
    IReadOnlyList<PublicPaymentReceiptSummary>? Receipts = null)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CardRequiredFields { get; init; }
}
