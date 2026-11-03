using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public sealed record JobMonitoringItem(
    Guid Id,
    string Type,
    string Key,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    double? DurationSeconds,
    string JobTitle);
