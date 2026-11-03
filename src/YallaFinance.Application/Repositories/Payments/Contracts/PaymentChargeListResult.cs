using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentChargeListResult(
    IReadOnlyList<PaymentChargeSnapshot> Items,
    int Page,
    int PageSize,
    int TotalItems,
    decimal TotalAmount,
    int AwaitingPaymentCount = 0,
    int ExpiredPaymentCount = 0,
    int AvailablePaymentCount = 0);
