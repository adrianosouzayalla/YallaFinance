using System.Globalization;

namespace YallaFinance.Application.Features.Jobs.Retry;

// Regra de retry automatico: sem configuracao, nenhum Job e retentado.
public static class JobRetryPolicy
{
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(15);

    // Valor ausente, invalido ou negativo significa zero retentativas.
    public static int ParseLimit(string? raw) =>
        int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var limit) && limit >= 0 ? limit : 0;

    // attempts inclui a execucao original. Com limite L, sao permitidas ate L retentativas.
    public static bool IsDue(int attempts, int limit, DateTimeOffset? lastFinishedAt, Guid jobId, DateTimeOffset now) =>
        lastFinishedAt is { } finishedAt
        && attempts >= 1
        && attempts <= limit
        && now >= finishedAt + DelayAfter(attempts, jobId);

    // Backoff exponencial a partir de BaseDelay, teto de MaxDelay e jitter deterministico de ate 20% por Job.
    public static TimeSpan DelayAfter(int attempts, Guid jobId)
    {
        var exponent = Math.Min(Math.Max(attempts - 1, 0), 10);
        var exponential = TimeSpan.FromTicks(BaseDelay.Ticks * (1L << exponent));
        var capped = exponential > MaxDelay ? MaxDelay : exponential;
        var jitterPercent = BitConverter.ToUInt32(jobId.ToByteArray(), 0) % 20;
        var withJitter = TimeSpan.FromTicks(capped.Ticks + capped.Ticks * jitterPercent / 100);
        return withJitter > MaxDelay ? MaxDelay : withJitter;
    }
}
