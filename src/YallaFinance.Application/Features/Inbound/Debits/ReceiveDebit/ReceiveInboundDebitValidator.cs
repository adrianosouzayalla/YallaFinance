using FluentValidation;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed class ReceiveInboundDebitValidator : AbstractValidator<ReceiveInboundDebitCommand>
{
    public ReceiveInboundDebitValidator(IValidator<ReceiveDebitPayload> payloadValidator)
    {
        RuleFor(command => command.Payload).NotNull().SetValidator(payloadValidator);
    }
}
