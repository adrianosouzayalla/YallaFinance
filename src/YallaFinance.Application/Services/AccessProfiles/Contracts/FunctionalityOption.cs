using YallaFinance.Application;
namespace YallaFinance.Application.Services.AccessProfiles.Contracts;

public sealed record FunctionalityOption(Guid Id, string Code, string Name, string Description);
