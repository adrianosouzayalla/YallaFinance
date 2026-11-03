using System.Linq.Expressions;
using YallaFinance.Infrastructure.Database.Generated;
using E = YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Repositories.Payments;

internal static class PaymentChargeEligibilityQuery
{
    public static Expression<Func<E.Charge, bool>> IsFinanciallyValid() => charge =>
        charge.ChargeItems.Any()
        && charge.ChargeItems.Select(item => item.Debt.ContractId).Distinct().Count() == 1
        && charge.Amount > 0
        && charge.Amount == charge.ChargeItems.Sum(item => item.Amount)
        && decimal.Round(charge.Amount, 2) == charge.Amount
        && !charge.ChargeItems.Any(item => item.TenantId != charge.TenantId
            || item.Debt.TenantId != charge.TenantId
            || item.Amount <= 0
            || item.Amount != item.Debt.Amount
            || (item.Debt.Status != "Open" && item.Debt.Status != "InCollection")
            || item.Debt.Contract.TenantId != charge.TenantId
            || !item.Debt.Contract.Active
            || item.Debt.Contract.Customer.TenantId != charge.TenantId
            || !item.Debt.Contract.Customer.Active
            || item.Debt.Category.TenantId != charge.TenantId
            || !item.Debt.Category.Active);

    public static Expression<Func<E.Charge, bool>> IsAvailable(YallaFinanceDbContext db, DateTime now)
    {
        var charge = Expression.Parameter(typeof(E.Charge), "charge");
        Expression<Func<E.Charge, bool>> predicate = row =>
            !db.PaymentAttempts.Any(attempt => attempt.TenantId == row.TenantId
                && attempt.ChargeId == row.Id && attempt.IssuanceBlocked
                && (attempt.Status != "AwaitingPayment" || attempt.ExpiresAt == null || attempt.ExpiresAt > now))
            && !db.PaymentRequests.Any(request => request.TenantId == row.TenantId
                && request.ChargeId == row.Id && request.Status == "Active" && request.ExpiresAt > now);
        var restrictionBody = ReplaceExpressionParameter(predicate, charge);
        var financialBody = ReplaceExpressionParameter(IsFinanciallyValid(), charge);
        return Expression.Lambda<Func<E.Charge, bool>>(
            Expression.AndAlso(restrictionBody, financialBody),
            charge);
    }

    private static Expression ReplaceExpressionParameter(LambdaExpression expression, ParameterExpression target) =>
        new ParameterReplaceVisitor(expression.Parameters[0], target).Visit(expression.Body)!;

    public static Expression<Func<E.Charge, object>> AsSortValue(YallaFinanceDbContext db, DateTime now)
    {
        var predicate = IsAvailable(db, now);
        return Expression.Lambda<Func<E.Charge, object>>(
            Expression.Convert(predicate.Body, typeof(object)), predicate.Parameters);
    }

    private sealed class ParameterReplaceVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
