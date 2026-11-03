using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed record JobHandlerRegistration(
    string Type,
    Func<string, CancellationToken, Task<JobHandlerResult>> Execute);
