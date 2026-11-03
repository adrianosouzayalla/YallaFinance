using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Payments.Contracts;

public sealed record PaymentChargeListFilter(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortDirection = "desc",
    string? PaymentStatus = null,
    string? Reference = null);
