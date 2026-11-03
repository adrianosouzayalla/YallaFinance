using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public sealed class JobReprocessingService(
    ICurrentTenantContext context,
    JobQueries queries,
    IJobRepository repository,
    TimeProvider timeProvider)
{
    public async Task<NotificationResult<JobReprocessingReceipt>> ReprocessAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(context.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return CreateFailureResult(ErrorCodes.Unauthorized, "Sessão sem tenant ativo.");
        var jobToReprocess = await queries.GetByIdAsync(tenantId, jobId, cancellationToken);
        if (jobToReprocess is null || jobToReprocess.TenantId != tenantId || jobToReprocess.Id != jobId)
            return CreateFailureResult(ErrorCodes.NotFound, "Job não encontrado.");
        if (!JobReprocessingEligibility.CanReprocess(jobToReprocess))
            return CreateFailureResult(
                ErrorCodes.Conflict,
                "Este Job não está com erro e não pode ser reprocessado.");
        if (await repository.TryRequeueAsync(
            tenantId,
            jobId,
            jobToReprocess.UpdatedAt,
            timeProvider.GetUtcNow(),
            cancellationToken))
            return NotificationResult<JobReprocessingReceipt>.Success(new("Job enviado para reprocessamento."));
        var latestJob = await queries.GetByIdAsync(tenantId, jobId, cancellationToken);
        return latestJob is null || latestJob.TenantId != tenantId || latestJob.Id != jobId
            ? CreateFailureResult(ErrorCodes.NotFound, "Job não encontrado.")
            : CreateFailureResult(
                ErrorCodes.Conflict,
                "O estado deste Job mudou. Atualize os dados antes de tentar novamente.");
    }

    private static NotificationResult<JobReprocessingReceipt> CreateFailureResult(string code, string message) =>
        NotificationResult<JobReprocessingReceipt>.Failure(new NotificationError(code, message));
}
