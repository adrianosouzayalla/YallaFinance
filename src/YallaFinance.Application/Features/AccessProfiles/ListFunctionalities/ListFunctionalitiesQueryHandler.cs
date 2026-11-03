using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.ListFunctionalities;

public sealed class ListFunctionalitiesQueryHandler(IAccessProfileRepository service)
    : IRequestHandler<ListFunctionalitiesQuery, ListFunctionalitiesResult>
{
    public async Task<ListFunctionalitiesResult> Handle(ListFunctionalitiesQuery request, CancellationToken cancellationToken) =>
        new(await service.ListFunctionalitiesAsync(cancellationToken));
}
