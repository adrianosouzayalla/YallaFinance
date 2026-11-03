using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Infrastructure.Database.Generated.Entities;
namespace YallaFinance.Infrastructure.Repositories.Debits;

public static class ReceiveDebitReplay
{
    public static bool Match(Debt existing, ReceiveDebitPayload payload) =>
        existing.Amount == payload.Debit.Amount
            && existing.OccurrenceDate == payload.Debit.OccurrenceDate
            && existing.DueDate == payload.Debit.DueDate
            && existing.Description == payload.Debit.Description
            && existing.Category.Code == payload.Debit.CategoryCode
            && existing.Contract.ExternalId == payload.Debit.Contract.ExternalId
            && existing.Contract.Customer.ExternalId == payload.Customer.ExternalId;
}
