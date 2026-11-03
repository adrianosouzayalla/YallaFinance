using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Dashboard.GetSummary;

public sealed class GetSummaryQueryHandler(IDebtRepository repository, ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<GetSummaryQuery, NotificationResult<DashboardSummaryResponse>>
{
    public async Task<NotificationResult<DashboardSummaryResponse>> Handle(
        GetSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<DashboardSummaryResponse>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        return NotificationResult<DashboardSummaryResponse>.Success(await repository.GetDashboardSummaryAsync(
            currentTenant.TenantId!,
            cancellationToken));
    }
}
