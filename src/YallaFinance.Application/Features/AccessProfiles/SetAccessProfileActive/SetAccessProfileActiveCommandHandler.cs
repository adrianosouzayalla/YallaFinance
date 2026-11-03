using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.SetAccessProfileActive;

public sealed class SetAccessProfileActiveCommandHandler(IAccessProfileRepository service)
    : IRequestHandler<SetAccessProfileActiveCommand>
{
    public Task Handle(SetAccessProfileActiveCommand request, CancellationToken cancellationToken) =>
        service.SetActiveAsync(request.Id, request.Active, cancellationToken);
}
