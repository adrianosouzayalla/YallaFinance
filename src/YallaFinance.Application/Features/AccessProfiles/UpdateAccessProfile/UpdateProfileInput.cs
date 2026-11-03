using YallaFinance.Application;
namespace YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;

public sealed record UpdateProfileInput(string Name, IReadOnlyList<Guid>? FunctionalityIds = null);
