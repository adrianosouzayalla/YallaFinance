using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application;



namespace YallaFinance.Application.Features.Debits.GetDebit;

public sealed class GetDebitQueryHandler(IDebtRepository repository, ICurrentTenantContext currentTenant)
    : MediatR.IRequestHandler<GetDebitQuery, NotificationResult<DebtResponse>>
{
    public async Task<NotificationResult<DebtResponse>> Handle(
        GetDebitQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTenant.TenantId))
            return NotificationResult<DebtResponse>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        var debitResponse = await repository.GetAsync(currentTenant.TenantId!, request.Id, cancellationToken);
        return debitResponse is null
            ? NotificationResult<DebtResponse>.Failure(new NotificationError(
                ErrorCodes.NotFound,
                "Débito não encontrado."))
            : NotificationResult<DebtResponse>.Success(debitResponse);
    }
}
