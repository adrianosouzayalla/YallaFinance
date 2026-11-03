using YallaFinance.Application.Services.AccessProfiles.Contracts;
using YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;
using YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;

namespace YallaFinance.Application.Repositories.AccessProfiles;

public interface IAccessProfileRepository
{
    Task<ProfilePage> ListAsync(ProfileFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<FunctionalityOption>> ListFunctionalitiesAsync(CancellationToken cancellationToken);
    Task<ProfileDetail> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ProfileDetail> CreateAsync(CreateProfileInput input, CancellationToken cancellationToken);
    Task<ProfileDetail> UpdateAsync(Guid id, UpdateProfileInput input, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken);
}
