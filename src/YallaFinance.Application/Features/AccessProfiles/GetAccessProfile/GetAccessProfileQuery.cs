using MediatR;
using YallaFinance.Application.Features.AccessProfiles.CreateAccessProfile;
using YallaFinance.Application.Features.AccessProfiles.ListAccessProfiles;
using YallaFinance.Application.Features.AccessProfiles.UpdateAccessProfile;
using YallaFinance.Application.Services.AccessProfiles.Contracts;
using YallaFinance.Application.Repositories.AccessProfiles;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.AccessProfiles.GetAccessProfile;

public sealed record GetAccessProfileQuery(Guid Id) : IRequest<GetAccessProfileResult>;
