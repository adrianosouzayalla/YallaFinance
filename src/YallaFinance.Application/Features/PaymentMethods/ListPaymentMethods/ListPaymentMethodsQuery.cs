using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.PaymentMethods.ListPaymentMethods;

public sealed record ListPaymentMethodsQuery : IRequest<NotificationResult<IReadOnlyList<PaymentMethodResponse>>>;
