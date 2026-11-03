using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Payments.GeneratePaymentRequests;

public sealed record PaymentRequestBatchItemResult(
    Guid ChargeId,
    PaymentRequestCreated? Request,
    string? ErrorCode = null,
    string? Message = null);
