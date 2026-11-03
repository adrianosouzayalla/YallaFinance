using MediatR;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;

public sealed class CreateAccessProfileCommandHandler(IAccessProfileRepository service)
    : IRequestHandler<CreateAccessProfileCommand, CreateAccessProfileResult>
{
    public async Task<CreateAccessProfileResult> Handle(CreateAccessProfileCommand request, CancellationToken cancellationToken) =>
        new(await service.CreateAsync(request.Input, cancellationToken));
}
