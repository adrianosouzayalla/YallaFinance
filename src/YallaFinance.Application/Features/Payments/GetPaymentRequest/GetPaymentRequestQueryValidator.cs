using FluentValidation;

namespace YallaFinance.Application.Features.Payments.GetPaymentRequest;

public sealed class GetPaymentRequestQueryValidator : AbstractValidator<GetPaymentRequestQuery>
{
    public GetPaymentRequestQueryValidator()
    {
        // The authenticated API supplies the tenant; the store owns missing-request responses.
    }
}
