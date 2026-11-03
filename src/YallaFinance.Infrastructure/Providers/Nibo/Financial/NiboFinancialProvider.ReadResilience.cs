using System.Net;
using Microsoft.Extensions.Logging;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial;

internal sealed partial class NiboFinancialProvider
{
    private async Task<HttpResponseMessage> SendReadAsync(
        Func<Task<HttpResponseMessage>> execute,
        string credential,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await coordinator.AcquireHttpSlotAsync(credential, cancellationToken);
            var delay = TimeSpan.FromMilliseconds(250 * (1 << (attempt - 1)) + Random.Shared.Next(250));
            try
            {
                var response = await execute();
                var retryAfter = response.Headers.RetryAfter;
                var advised = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    coordinator.DelayRequests(credential, advised ?? TimeSpan.FromSeconds(1));
                if (response.IsSuccessStatusCode || attempt == maximumAttempts ||
                    !((int)response.StatusCode >= 500 ||
                        response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout))
                    return response;
                // A long Retry-After is left for the durable scheduler instead of keeping an HTTP request waiting.
                if (advised > TimeSpan.FromSeconds(60))
                    return response;
                if (advised > delay)
                    delay = advised.Value;
                logger.LogWarning("Nibo read will retry after HTTP {Status}; attempt {Attempt}; delay {DelayMs} ms",
                    (int)response.StatusCode, attempt, delay.TotalMilliseconds);
                response.Dispose();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                logger.LogWarning(
                    "Nibo read transport failure {ExceptionType}; transport error {TransportError}; attempt {Attempt}",
                    exception.GetType().Name,
                    (exception as HttpRequestException)?.HttpRequestError.ToString(),
                    attempt);
                if (attempt == maximumAttempts)
                    throw;
            }
            await Task.Delay(delay, cancellationToken);
        }
    }
}
