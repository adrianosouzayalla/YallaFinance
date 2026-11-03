using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;

public sealed class ListAccessProfilesQueryHandler(IAccessProfileRepository service)
    : IRequestHandler<ListAccessProfilesQuery, ListAccessProfilesResult>
{
    public async Task<ListAccessProfilesResult> Handle(ListAccessProfilesQuery request, CancellationToken cancellationToken) =>
        new(await service.ListAsync(request.Filter, cancellationToken));
}
