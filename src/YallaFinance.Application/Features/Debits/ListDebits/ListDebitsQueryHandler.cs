using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Debits.ListDebits;

public sealed class ListDebitsQueryHandler(IDebtRepository repository, ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<ListDebitsQuery, NotificationResult<DebtListResult>>
{
    public async Task<NotificationResult<DebtListResult>> Handle(
        ListDebitsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<DebtListResult>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        return NotificationResult<DebtListResult>.Success(await repository.ListAsync(
            currentTenant.TenantId!,
            request.Filters,
            cancellationToken));
    }
}
