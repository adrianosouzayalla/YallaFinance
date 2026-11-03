using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Services.Payments.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Payments.StartPublicPaymentAttempt;

public sealed class StartPublicPaymentAttemptCommandHandler(IPublicPaymentAttemptService service)
    : IRequestHandler<StartPublicPaymentAttemptCommand, NotificationResult<PublicPaymentAttemptResult>>
{
    public Task<NotificationResult<PublicPaymentAttemptResult>> Handle(
        StartPublicPaymentAttemptCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsToken(command.Token) || string.IsNullOrWhiteSpace(command.MethodCode)
            || command.MethodCode.Length > 64 || string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || command.IdempotencyKey.Length > 200)
        {
            return Task.FromResult(NotificationResult<PublicPaymentAttemptResult>.Failure(
                new NotificationError(
                    ErrorCodes.Validation,
                    "Não foi possível iniciar este pagamento. Confira os dados e tente novamente.")));
        }

        return service.StartOrGetAsync(
            command.Token,
            command.MethodCode,
            command.IdempotencyKey,
            cancellationToken,
            command.Card);
    }

    private static bool IsToken(string? token) => token is { Length: 64 }
        && token.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
