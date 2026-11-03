using YallaFinance.Application;
namespace YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;

public sealed record CreateProfileInput(string Code, string Name, bool Active = true, IReadOnlyList<Guid>? FunctionalityIds = null);
