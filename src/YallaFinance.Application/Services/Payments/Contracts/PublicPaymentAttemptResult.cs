using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Contracts;

public sealed record PublicPaymentAttemptResult(
    string Status,
    decimal Amount,
    DateTime? ExpiresAt,
    string? BrCode,
    string? Location,
    bool CanRetry,
    string? Message,
    string? PaymentMethodCode = null)
{
    public string? InstrumentType { get; init; }
    public string? AttemptReference { get; init; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public YallaFinance.Application.Services.Payments.Boleto.Contracts.PublicBoletoDetails? Boleto { get; init; }
}
