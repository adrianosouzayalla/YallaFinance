using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Payments.ListPaymentCharges;

public sealed record PaymentChargesQuery(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortDirection = "desc",
    string? PaymentStatus = null,
    string? Reference = null)
    : IRequest<NotificationResult<PaymentChargeListResult>>;
