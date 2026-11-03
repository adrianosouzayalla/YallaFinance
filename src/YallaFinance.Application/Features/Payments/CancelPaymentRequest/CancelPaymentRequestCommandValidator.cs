using FluentValidation;

namespace YallaFinance.Application.Features.Payments.CancelPaymentRequest;

public sealed class CancelPaymentRequestCommandValidator : AbstractValidator<CancelPaymentRequestCommand>
{
    public CancelPaymentRequestCommandValidator()
    {
        // Tenant and actor come from authenticated claims; the store owns request lookup semantics.
    }
}
