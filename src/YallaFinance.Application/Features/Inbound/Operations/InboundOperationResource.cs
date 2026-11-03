using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed record InboundOperationResource(string Type, Guid Id);
