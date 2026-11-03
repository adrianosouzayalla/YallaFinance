using System.Text.Json;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public sealed record JobMonitoringExecution(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    double DurationSeconds,
    string Status,
    string? ErrorCode,
    JsonElement? Result,
    JsonElement? Details);
