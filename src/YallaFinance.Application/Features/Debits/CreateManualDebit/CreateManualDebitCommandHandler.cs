using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Debits.CreateManualDebit;

public sealed class CreateManualDebitCommandHandler(
    ICurrentTenantContext context,
    IDebtRepository debts,
    IValidator<CreateManualDebitCommand> validator) : IRequestHandler<CreateManualDebitCommand, NotificationResult<CreateManualDebitResult>>
{
    public async Task<NotificationResult<CreateManualDebitResult>> Handle(
        CreateManualDebitCommand request,
        CancellationToken token)
    {
        if (!Guid.TryParse(context.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return NotificationResult<CreateManualDebitResult>.Failure(new NotificationError(
                ErrorCodes.Forbidden,
                "Sessão sem tenant ativo."));
        if (!(await validator.ValidateAsync(request, token)).IsValid)
            return NotificationResult<CreateManualDebitResult>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Confira os campos do débito."));
        return await debts.CreateManualAsync(tenantId, request, token);
    }
}
