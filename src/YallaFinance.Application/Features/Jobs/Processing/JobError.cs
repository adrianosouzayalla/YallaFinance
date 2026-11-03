using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

// Only public, controlled messages belong here; never use exception/HTTP response text.
public sealed record JobError(string Code, string Message);
