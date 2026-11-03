

using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Repositories.IntegrationOperations.Contracts;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Operations;

public static class InboundOperationProjector
{
    public static InboundOperationResponse Project(IntegrationOperationSnapshot snapshot)
    {
        var integrationOperation = snapshot.Operation;
        var operationStatus = snapshot.JobStatus switch
        {
            JobStatus.Pending => "accepted",
            JobStatus.Processing => "processing",
            JobStatus.Success => "completed",
            JobStatus.Failed => "failed",
            _ => throw new InvalidOperationException("Invalid operation job status.")
        };
        var operationIsFinished = snapshot.JobStatus is JobStatus.Success or JobStatus.Failed;
        if (operationIsFinished &&
            (integrationOperation.CompletedAt is null ||
                (snapshot.JobStatus == JobStatus.Success &&
                    (integrationOperation.ResourceType != "Debit" ||
                        integrationOperation.ResourceId is null ||
                        integrationOperation.ResourceId == Guid.Empty))))
            throw new InvalidOperationException("Operation completion is inconsistent.");
        InboundOperationError? error = null;
        if (snapshot.JobStatus == JobStatus.Failed)
        {
            var errorCode = JobMonitoringResultProjector.Error(snapshot.ExecutionError);
            var errorMessage = errorCode switch
            {
                "InboundCategoryUnavailable" => "Categoria indisponível para o tenant.",
                "InboundTenantUnavailable" => "Tenant indisponível para o processamento.",
                "InboundCustomerConflict" => "Dados do cliente exigem conferência.",
                "InboundContractConflict" => "Dados do contrato exigem conferência.",
                "InboundDebitConflict" => "Identificador de débito utilizado com dados diferentes.",
                "InvalidDebitPayload" or "InvalidJobPayload" => "Contrato do débito inválido.",
                _ => "Não foi possível concluir o processamento da operação."
            };
            error = new(errorCode, errorMessage);
        }
        return new(
            integrationOperation.Id,
            operationStatus,
            integrationOperation.Type,
            integrationOperation.CreatedAt,
            operationIsFinished ? integrationOperation.CompletedAt : null,
            snapshot.JobStatus == JobStatus.Success ? new(integrationOperation.ResourceType!, integrationOperation.ResourceId!.Value) : null,
            error);
    }
}
