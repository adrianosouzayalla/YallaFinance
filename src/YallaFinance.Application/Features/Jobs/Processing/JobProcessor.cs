using Microsoft.Extensions.Logging;
using System.Text.Json;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class JobProcessor(
    IJobRepository jobs,
    IJobExecutionRepository executions,
    IJobRegistry registry,
    IJobHandlerExecutor handlerExecutor,
    IJobTenantContext tenant,
    JobExecutionContext executionContext,
    TimeProvider timeProvider,
    ILogger<JobProcessor> logger)
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var claimedJob = await jobs.TryClaimNextAsync(cancellationToken);

        if (claimedJob is null)
            return false;

        tenant.Initialize(claimedJob.Job.TenantId);

        executionContext.Initialize(claimedJob.Job.TenantId, claimedJob.Job.Id, claimedJob.Execution.Id);

        var jobStartedTimestamp = timeProvider.GetTimestamp();

        logger.LogInformation(
            "Job started TenantId={TenantId} JobId={JobId} JobExecutionId={JobExecutionId} Type={Type}",
            claimedJob.Job.TenantId,
            claimedJob.Job.Id,
            claimedJob.Execution.Id,
            claimedJob.Job.Type);

        JobHandlerResult handlerResult;

        try
        {
            // Após o claim, o Type seleciona o handler; o registro desserializa o Payload.
            if (!registry.Contains(claimedJob.Job.Type))
                handlerResult = new(false, Error: new("UnknownJobType", "Job type is not registered."));
            else
                handlerResult = await handlerExecutor.ExecuteAsync(
                    claimedJob.Job.Type,
                    claimedJob.Job.Payload,
                    cancellationToken);
            if (handlerResult is null ||
                (!handlerResult.Success &&
                    (handlerResult.Error is null ||
                        string.IsNullOrWhiteSpace(handlerResult.Error.Code))))
                handlerResult = new(false, Error: new("InvalidHandlerResult", "Handler returned an invalid result."));
            else if (handlerResult.Success)
                handlerResult = handlerResult with
                {
                    Error = null
                };
            try
            {
                handlerResult = handlerResult with
                {
                    Result = Snapshot(handlerResult.Result),
                    Details = Snapshot(handlerResult.Details)
                };
            }
            catch (InvalidOperationException)
            {
                handlerResult = new(
                    false,
                    Error: new(
                        "InvalidHandlerResult",
                        "Handler returned an invalid JSON result."));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidJobPayloadException exception)
        {
            handlerResult = new(false, Error: new("InvalidJobPayload", "Job payload cannot be reconstructed."),
                Details: ExceptionDetails("PayloadReconstruction", exception));
        }
        catch (Exception exception)
        {
            handlerResult = new(false, Error: new("JobHandlerError", "Handler could not complete the operation."),
                Details: ExceptionDetails("HandlerExecution", exception));
        }

        if (handlerResult.Success && handlerResult.Result is null && handlerResult.Details is null)
            handlerResult = handlerResult with
            {
                Result = JsonSerializer.SerializeToElement(new
                {
                    outcome = "Succeeded"
                })
            };
        else if (!handlerResult.Success && handlerResult.Details is null)
            handlerResult = handlerResult with
            {
                Details = JsonSerializer.SerializeToElement(new
                {
                    diagnostic = "HandlerReturnedFailure",
                    errorCode = handlerResult.Error?.Code,
                    errorMessage = handlerResult.Error?.Message
                })
            };

        // Cancellation/DB failures here leave the committed attempt open; no automatic recovery.
        await executions.CompleteAsync(
            claimedJob.Job.TenantId,
            claimedJob.Job.Id,
            claimedJob.Execution.Id,
            handlerResult,
            cancellationToken);

        logger.LogInformation(
            "Job completed TenantId={TenantId} JobId={JobId} JobExecutionId={JobExecutionId} Type={Type} DurationMs={DurationMs} Success={Success}",
            claimedJob.Job.TenantId,
            claimedJob.Job.Id,
            claimedJob.Execution.Id,
            claimedJob.Job.Type,
            timeProvider.GetElapsedTime(jobStartedTimestamp).TotalMilliseconds,
            handlerResult.Success);
        return true;
    }

    private static JsonElement? Snapshot(JsonElement? element)
    {
        if (element is null)
            return null;
        if (element.Value.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("Undefined handler JSON.");
        return element.Value.Clone();
    }

    private static JsonElement ExceptionDetails(string stage, Exception exception) =>
        JsonSerializer.SerializeToElement(new
        {
            diagnostic = "UnhandledJobException",
            stage,
            exceptionType = exception.GetType().FullName,
            rootExceptionType = exception.GetBaseException().GetType().FullName,
            exception.HResult
        });
}
