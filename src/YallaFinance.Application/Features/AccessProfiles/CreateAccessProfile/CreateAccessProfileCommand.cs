using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;

public sealed record CreateAccessProfileCommand(CreateProfileInput Input) : IRequest<CreateAccessProfileResult>;
