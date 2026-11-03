using System.Text.Json;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed record JobHandlerResult(
    bool Success,
    JsonElement? Result = null,
    JobError? Error = null,
    JsonElement? Details = null);
