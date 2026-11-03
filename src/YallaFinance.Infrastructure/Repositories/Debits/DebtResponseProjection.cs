using System.Linq.Expressions;

using YallaFinance.Domain.Debts;
using Debt = YallaFinance.Infrastructure.Database.Generated.Entities.Debt;
using YallaFinance.Application.Repositories.Debits.Contracts;

namespace YallaFinance.Infrastructure.Repositories.Debits;

public static class DebtResponseProjection
{
    public static readonly Expression<Func<Debt, DebtResponse>> Select = debt => new DebtResponse(
        debt.Id.ToString(),
        debt.CategoryId.ToString(),
        debt.TenantId.ToString(),
        debt.Contract.Customer.Name,
        debt.Contract.Number,
        debt.Contract.Product ?? "",
        debt.Contract.Plate ?? "",
        debt.Contract.Fleet ?? "",
        debt.OccurrenceDate,
        debt.Amount,
        Enum.Parse<DebtStatus>(debt.Status),
        debt.Description,
        debt.Origin,
        debt.ChargeItem != null && debt.ChargeItem.TenantId == debt.TenantId
            && debt.ChargeItem.Charge.TenantId == debt.TenantId
            && !string.IsNullOrWhiteSpace(debt.ChargeItem.ProviderExternalId)
                ? debt.ChargeItem.ProviderExternalId : null,
        debt.DueDate,
        debt.Contract.CustomerId.ToString(),
        debt.ChargeItem != null && debt.ChargeItem.TenantId == debt.TenantId
            && debt.ChargeItem.Charge.TenantId == debt.TenantId
                ? debt.ChargeItem.ChargeId.ToString() : null);
}
