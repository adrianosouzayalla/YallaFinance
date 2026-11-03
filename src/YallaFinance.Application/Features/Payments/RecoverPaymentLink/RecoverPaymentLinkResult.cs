using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.RecoverPaymentLink;

public sealed record RecoverPaymentLinkResult(NotificationResult<PaymentLinkResult> Response);
