using System.Text.Json;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public sealed record JobMonitoringDetail(
    Guid Id,
    string Type,
    string Key,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    JsonElement? Payload,
    string JobTitle);
