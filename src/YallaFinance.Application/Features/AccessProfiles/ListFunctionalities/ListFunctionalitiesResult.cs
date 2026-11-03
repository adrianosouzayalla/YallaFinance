using YallaFinance.Application.Services.AccessProfiles.Contracts;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.AccessProfiles.ListFunctionalities;

public sealed record ListFunctionalitiesResult(IReadOnlyList<FunctionalityOption> Functionalities);
