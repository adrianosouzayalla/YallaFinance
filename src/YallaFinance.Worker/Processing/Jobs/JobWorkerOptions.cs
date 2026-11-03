namespace YallaFinance.Worker.Processing.Jobs;

public sealed class JobWorkerOptions
{
    public int BatchSize { get; set; } = 10;
    public int PollingIntervalSeconds { get; set; } = 5;
}
