using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentReceiptSummary(
    Guid Id,
    string? EndToEndId,
    decimal Amount,
    DateTime ReceivedAt,
    string PaymentMethodName,
    string Provider)
{
    public string? ProviderTransactionId { get; init; }
}
