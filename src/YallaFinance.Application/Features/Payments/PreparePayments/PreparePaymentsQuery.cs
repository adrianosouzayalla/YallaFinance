using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Payments.PreparePayments;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Payments.PreparePayments;

public sealed record PreparePaymentsQuery(IReadOnlyList<Guid> ChargeIds) : IRequest<NotificationResult<PaymentPreparation>>;
