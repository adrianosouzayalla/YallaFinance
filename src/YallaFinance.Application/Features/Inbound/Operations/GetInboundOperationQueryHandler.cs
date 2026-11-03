using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.IntegrationOperations;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed class GetInboundOperationQueryHandler(
    ICurrentTenantContext context,
    IIntegrationOperationRepository operations) : IRequestHandler<GetInboundOperationQuery, NotificationResult<InboundOperationResponse>>
{
    public async Task<NotificationResult<InboundOperationResponse>> Handle(
        GetInboundOperationQuery request,
        CancellationToken token)
    {
        if (!Guid.TryParse(context.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return NotificationResult<InboundOperationResponse>.Failure(new NotificationError(
                ErrorCodes.Forbidden,
                "Sessão sem tenant autorizado."));
        if (request.OperationId == Guid.Empty)
            return NotificationResult<InboundOperationResponse>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Identificador de operação inválido."));
        var operationSnapshot = await operations.GetAsync(tenantId, request.OperationId, token);
        return operationSnapshot is null ||
            operationSnapshot.Operation.TenantId != tenantId ||
            operationSnapshot.Operation.Id != request.OperationId
                ? NotificationResult<InboundOperationResponse>.Failure(new NotificationError(
                    ErrorCodes.NotFound,
                    "Operação não encontrada."))
                : NotificationResult<InboundOperationResponse>.Success(InboundOperationProjector.Project(operationSnapshot));
    }
}
