using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public sealed record JobRegistration(
    string Type,
    Type PayloadType,
    string JobTitle);
