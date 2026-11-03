using System.Text.Json;
using FluentValidation;

using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed class ReceiveDebitHandler(
    ICurrentTenantContext tenant,
    IValidator<ReceiveDebitPayload> validator,
    IDebtRepository debts) : IJobHandler<ReceiveDebitPayload>
{
    public async Task<JobHandlerResult> HandleAsync(
        ReceiveDebitPayload payload,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(tenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return new(
                false,
                Error: new(
                    "InboundTenantUnavailable",
                    "Tenant indisponível para o processamento."));
        if (payload is null || !(await validator.ValidateAsync(payload,
                options => options.IncludeRuleSets("StoredJob"), cancellationToken)).IsValid)
            return new(false, Error: new("InvalidDebitPayload", "Confira o contrato de entrada do débito."));
        var debitCreationResult = await debts.ReceiveAsync(tenantId, payload, cancellationToken);
        if (debitCreationResult.IsFailure)
        {
            var debitCreationError = debitCreationResult.Errors.First();
            return new(false, Error: new(debitCreationError.Code, debitCreationError.Message));
        }
        return new(true, JsonSerializer.SerializeToElement(debitCreationResult.Value, JsonSerializerOptions.Web));
    }
}
