using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed record InboundOperationError(string Code, string Message);
