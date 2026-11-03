using YallaFinance.Application;
namespace YallaFinance.Application.Services.AccessProfiles.Contracts;

public sealed record ProfileDetail(Guid Id, string Code, string Name, bool Active, IReadOnlyList<Guid>? FunctionalityIds = null);
