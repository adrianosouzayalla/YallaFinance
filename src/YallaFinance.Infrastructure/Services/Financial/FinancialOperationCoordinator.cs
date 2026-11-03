namespace YallaFinance.Infrastructure.Services.Financial;

internal sealed class FinancialOperationCoordinator(int requestIntervalMilliseconds = 100) : IDisposable
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> httpQueues = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTimeOffset> requestTimes = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> requestIntervals = new();

    public async Task AcquireHttpSlotAsync(string credential, CancellationToken cancellationToken)
    {
        var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(credential)));
        var queue = httpQueues.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await queue.WaitAsync(cancellationToken);
        try
        {
            while (requestTimes.TryGetValue(key, out var next))
            {
                var remaining = next - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;
                await Task.Delay(remaining, cancellationToken);
            }
            var interval = requestIntervals.GetOrAdd(key, requestIntervalMilliseconds);
            var following = DateTimeOffset.UtcNow.AddMilliseconds(interval);
            requestTimes.AddOrUpdate(
                key,
                following,
                (_, previous) => previous > following ? previous : following);
        }
        finally
        {
            queue.Release();
        }
    }

    public void DelayRequests(string credential, TimeSpan delay)
    {
        var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(credential)));
        var following = DateTimeOffset.UtcNow.Add(delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1));
        requestTimes.AddOrUpdate(key, following, (_, previous) => previous > following ? previous : following);
        // Reduce pressure after a 429; never speed up automatically during the same execution.
        requestIntervals.AddOrUpdate(key, Math.Min(
            Math.Max(1000, requestIntervalMilliseconds),
            requestIntervalMilliseconds + Math.Max(
                25,
                requestIntervalMilliseconds / 4)),
            (_, previous) => Math.Min(
                Math.Max(1000, requestIntervalMilliseconds),
                previous + Math.Max(
                    25,
                    previous / 4)));
    }

    private readonly SemaphoreSlim[] charges = Enumerable.Range(0, 1024).Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();
    private readonly SemaphoreSlim[] customers = Enumerable.Range(0, 1024).Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    public Task<SemaphoreSlim> AcquireChargeAsync(
        Guid tenant,
        Guid connection,
        Guid charge,
        CancellationToken cancellationToken)
        => AcquireAsync(charges, HashCode.Combine(tenant, connection, charge), cancellationToken);

    public Task<SemaphoreSlim> AcquireCustomerAsync(
        Guid tenant,
        Guid connection,
        string document,
        CancellationToken cancellationToken)
        => AcquireAsync(customers, HashCode.Combine(tenant, connection, document), cancellationToken);

    private static async Task<SemaphoreSlim> AcquireAsync(
        SemaphoreSlim[] gates,
        int hash,
        CancellationToken cancellationToken)
    {
        var gate = gates[(int)((uint)hash % (uint)gates.Length)];
        await gate.WaitAsync(cancellationToken);
        return gate;
    }

    public void Dispose()
    {
        foreach (var gate in charges.Concat(customers))
            gate.Dispose();
        foreach (var queue in httpQueues.Values)
            queue.Dispose();
    }
}
