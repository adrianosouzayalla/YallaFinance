namespace YallaFinance.WebApi.Security;

/// <summary>Sliding-window attempt limit. Callers decide what to do when the limit is reached.</summary>
public sealed record AttemptRateRule(int Limit, TimeSpan Window);

/// <summary>
/// Counts recorded attempts per key inside a sliding window. Thread-safe.
/// Each company has its own window (at most 1440 minutes), so expired keys are swept only when every attempt is older than
/// the largest window allowed.
/// </summary>
public sealed class AttemptRateObserver
{
    private const int SweepInterval = 1000;
    private static readonly TimeSpan LargestAllowedWindow = TimeSpan.FromMinutes(1440);

    private readonly Dictionary<string, Queue<DateTimeOffset>> attemptsByKey = new(StringComparer.Ordinal);
    private readonly Lock sync = new();
    private int recordsSinceSweep;

    /// <summary>True when the attempts recorded inside the window already reach the rule's limit.</summary>
    public bool HasReachedLimit(string key, AttemptRateRule rule, DateTimeOffset now)
    {
        lock (sync)
        {
            if (!attemptsByKey.TryGetValue(key, out var attempts))
                return false;

            RemoveAttemptsOutsideWindow(attempts, rule.Window, now);
            return attempts.Count >= rule.Limit;
        }
    }

    /// <summary>Records one attempt for the key.</summary>
    public void RecordAttempt(string key, AttemptRateRule rule, DateTimeOffset now)
    {
        lock (sync)
        {
            if (++recordsSinceSweep >= SweepInterval)
            {
                recordsSinceSweep = 0;
                RemoveKeysWithoutRecentAttempts(now);
            }

            if (!attemptsByKey.TryGetValue(key, out var attempts))
            {
                attempts = new Queue<DateTimeOffset>();
                attemptsByKey[key] = attempts;
            }

            RemoveAttemptsOutsideWindow(attempts, rule.Window, now);
            attempts.Enqueue(now);
        }
    }

    private static void RemoveAttemptsOutsideWindow(Queue<DateTimeOffset> attempts, TimeSpan window, DateTimeOffset now)
    {
        var windowStart = now - window;
        while (attempts.Count > 0 && attempts.Peek() <= windowStart)
            attempts.Dequeue();
    }

    private void RemoveKeysWithoutRecentAttempts(DateTimeOffset now)
    {
        var windowStart = now - LargestAllowedWindow;
        var expiredKeys = attemptsByKey
            .Where(pair => pair.Value.All(attempt => attempt <= windowStart))
            .Select(pair => pair.Key)
            .ToList();
        foreach (var expiredKey in expiredKeys)
            attemptsByKey.Remove(expiredKey);
    }
}
