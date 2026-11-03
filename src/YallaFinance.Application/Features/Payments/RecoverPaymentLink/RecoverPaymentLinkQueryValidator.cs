using FluentValidation;

namespace YallaFinance.Application.Features.Payments.RecoverPaymentLink;

public sealed class RecoverPaymentLinkQueryValidator : AbstractValidator<RecoverPaymentLinkQuery>
{
    public RecoverPaymentLinkQueryValidator()
    {
        // The authenticated API supplies the tenant; the store owns missing-request responses.
    }
}
