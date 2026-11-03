using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;

public sealed class UpdateAccessProfileCommandHandler(IAccessProfileRepository service)
    : IRequestHandler<UpdateAccessProfileCommand, UpdateAccessProfileResult>
{
    public async Task<UpdateAccessProfileResult> Handle(UpdateAccessProfileCommand request, CancellationToken cancellationToken) =>
        new(await service.UpdateAsync(request.Id, request.Input, cancellationToken));
}
