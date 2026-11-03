using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentAttemptActivitySummary(
    string Status,
    string Provider,
    string PaymentMethodName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ExpiresAt,
    bool IssuanceBlocked,
    string? FailureCode,
    string? FailureMessage)
{
    public string? BoletoPdfUrl { get; init; }
}
