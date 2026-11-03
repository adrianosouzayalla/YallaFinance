using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Payments.StartPublicPaymentAttempt;

public sealed record StartPublicPaymentAttemptCommand(string Token, string MethodCode, string IdempotencyKey)
    : IRequest<NotificationResult<PublicPaymentAttemptResult>>
{
    public PublicCardPaymentInput? Card { get; init; }
    public override string ToString() => nameof(StartPublicPaymentAttemptCommand);
}
