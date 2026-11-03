using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.GetAccessProfile;

public sealed class GetAccessProfileQueryHandler(IAccessProfileRepository service)
    : IRequestHandler<GetAccessProfileQuery, GetAccessProfileResult>
{
    public async Task<GetAccessProfileResult> Handle(GetAccessProfileQuery request, CancellationToken cancellationToken) =>
        new(await service.GetAsync(request.Id, cancellationToken));
}
