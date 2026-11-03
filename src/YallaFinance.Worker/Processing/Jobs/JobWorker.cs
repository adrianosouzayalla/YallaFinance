using Microsoft.Extensions.Options;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Retry;


namespace YallaFinance.Worker.Processing.Jobs;

public sealed class JobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<JobWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jobWorkerOptions = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            var shouldWaitForNextPollingCycle = false;
            try
            {
                await RequeueDueJobsAsync(stoppingToken);
                for (var jobIndexInBatch = 0;
                     jobIndexInBatch < jobWorkerOptions.BatchSize;
                     jobIndexInBatch++)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var systemId = await scope.ServiceProvider
                        .GetRequiredService<YallaFinance.Application.Repositories.Users.IUserIdentityRepository>()
                        .FindOperationalUserIdAsync("system", stoppingToken)
                        ?? throw new InvalidOperationException("The operational SYSTEM identity is unavailable.");
                    scope.ServiceProvider.GetRequiredService<YallaFinance.Application.Common.Context.CurrentExecutionActorContext>()
                        .SetOperationalUser(systemId);
                    var processor = scope.ServiceProvider.GetRequiredService<JobProcessor>();
                    if (!await processor.ProcessNextAsync(stoppingToken))
                    {
                        shouldWaitForNextPollingCycle = true;
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Keep infrastructure diagnostics useful without exposing exception messages or connection data.
                logger.LogError(
                    "Job processing infrastructure failed. ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}; HResult {HResult}. Next poll will follow the configured interval.",
                    exception.GetType().FullName,
                    exception.GetBaseException().GetType().FullName,
                    exception.HResult);
                shouldWaitForNextPollingCycle = true;
            }
            if (shouldWaitForNextPollingCycle)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(jobWorkerOptions.PollingIntervalSeconds),
                        timeProvider,
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task RequeueDueJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var systemId = await scope.ServiceProvider
                .GetRequiredService<YallaFinance.Application.Repositories.Users.IUserIdentityRepository>()
                .FindOperationalUserIdAsync("system", stoppingToken)
                ?? throw new InvalidOperationException("The operational SYSTEM identity is unavailable.");
            scope.ServiceProvider.GetRequiredService<YallaFinance.Application.Common.Context.CurrentExecutionActorContext>()
                .SetOperationalUser(systemId);
            var requeued = await scope.ServiceProvider.GetRequiredService<JobRetrySweeper>()
                .RequeueDueJobsAsync(stoppingToken);
            if (requeued > 0)
                logger.LogInformation("Job retry requeued {Requeued} job(s).", requeued);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Job retry sweep failed. ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                exception.GetType().FullName,
                exception.GetBaseException().GetType().FullName);
        }
    }
}
