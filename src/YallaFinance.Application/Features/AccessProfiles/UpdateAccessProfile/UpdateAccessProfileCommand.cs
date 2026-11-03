using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;

public sealed record UpdateAccessProfileCommand(Guid Id, UpdateProfileInput Input) : IRequest<UpdateAccessProfileResult>;
