using FluentValidation;
using YallaFinance.Domain.Charges;

namespace YallaFinance.Application.Features.Payments.ListPaymentCharges;

public sealed class PaymentChargesQueryValidator : AbstractValidator<PaymentChargesQuery>
{
    public PaymentChargesQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000)
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100)
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query).Must(query => query.StartDate is null || query.EndDate is null ||
                query.StartDate <= query.EndDate)
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.EndDate).Must(date => date != DateOnly.MaxValue)
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.SortDirection).Must(direction => direction is "asc" or "desc")
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.SortBy).Must(sort => sort is null or "charge" or "customer" or "contract" or
            "created" or "due" or "amount" or "eligibility")
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.PaymentStatus).Must(status => status is null or "awaitingPayment" or "expired" or "available")
            .WithMessage("Confira o período e a paginação.");
        RuleFor(query => query.Reference).Must(reference => reference is null ||
            ChargeReference.TryParse(reference, out _, out _))
            .WithMessage("Informe a cobrança no formato empresa-número, como 10-123.");
    }
}
