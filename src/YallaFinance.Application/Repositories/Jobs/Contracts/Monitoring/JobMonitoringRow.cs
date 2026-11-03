using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Jobs.Contracts.Monitoring;

public sealed record JobMonitoringRow(
    Guid Id,
    string Type,
    string Key,
    JobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt);
