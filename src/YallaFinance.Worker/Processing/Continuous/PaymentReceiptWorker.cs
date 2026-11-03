using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Payments;

namespace YallaFinance.Worker.Processing.Continuous;

public sealed class PaymentReceiptWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<PaymentReceiptWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var systemId = await scope.ServiceProvider
                    .GetRequiredService<IUserIdentityRepository>()
                    .FindOperationalUserIdAsync("system", stoppingToken)
                    ?? throw new InvalidOperationException("The operational SYSTEM identity is unavailable.");
                scope.ServiceProvider.GetRequiredService<CurrentExecutionActorContext>()
                    .SetOperationalUser(systemId);
                var paymentReceiptSynchronizationService = scope.ServiceProvider
                    .GetRequiredService<IPaymentReceiptSynchronizationService>();
                await paymentReceiptSynchronizationService.SynchronizeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Payment receipt synchronization failed. ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                    exception.GetType().Name,
                    exception.GetBaseException().GetType().Name);
            }

            try
            {
                await Task.Delay(PollingInterval, clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
