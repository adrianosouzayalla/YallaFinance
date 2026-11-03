using YallaFinance.Application.Common.Context;

namespace YallaFinance.Application.Repositories.Authorization;

public interface IFunctionalityAccessRepository
{
    Task<bool> HasAnyAsync(SessionContext session, IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListGrantedCodesAsync(SessionContext session, CancellationToken cancellationToken);
}
