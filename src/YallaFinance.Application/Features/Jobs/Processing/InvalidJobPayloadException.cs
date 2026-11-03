using System.Text.Json;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed class InvalidJobPayloadException() : JsonException("Job payload cannot be reconstructed.");
