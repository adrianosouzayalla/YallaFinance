using YallaFinance.Application.Common.Errors.Access;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.AccessProfiles.Shared;

public static class AccessProfileFunctionalityRules
{
    public static IReadOnlyList<Guid> ResolveSelection(
        IReadOnlyCollection<Guid>? requestedIds,
        IReadOnlyCollection<Guid> activeIds,
        IReadOnlyCollection<Guid> existingInactiveGrantIds)
    {
        var requestedFunctionalityIds = requestedIds ?? [];
        if (requestedFunctionalityIds.Count != requestedFunctionalityIds.Distinct().Count())
            throw new UserManagementException("INVALID_FUNCTIONALITY", "A lista de funcionalidades contém itens repetidos.");

        var activeFunctionalityIds = activeIds.ToHashSet();
        if (requestedFunctionalityIds.Any(functionalityId => !activeFunctionalityIds.Contains(functionalityId)))
            throw new UserManagementException("INVALID_FUNCTIONALITY", "Uma ou mais funcionalidades não existem ou estão inativas.");

        return requestedFunctionalityIds.Concat(existingInactiveGrantIds).Distinct().ToArray();
    }
}
