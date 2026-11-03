using MediatR;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;

public sealed record ListAccessProfilesQuery(ProfileFilter Filter) : IRequest<ListAccessProfilesResult>;
