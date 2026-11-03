using YallaFinance.Domain.Integrations;
using YallaFinance.Domain.Jobs;

namespace YallaFinance.Application.Repositories.IntegrationOperations.Contracts;

public sealed record IntegrationOperationSnapshot(
    IntegrationOperation Operation,
    JobStatus JobStatus,
    string? ExecutionError);
