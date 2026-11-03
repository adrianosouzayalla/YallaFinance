using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentRequestActivitySummary(
    Guid Id,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ExpiredAt,
    DateTime? CanceledAt,
    DateTime? CompletedAt,
    string? CompletionReason,
    IReadOnlyList<string>? AuthorizedPaymentMethods = null);
