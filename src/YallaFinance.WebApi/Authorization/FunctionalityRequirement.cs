using Microsoft.AspNetCore.Authorization;

namespace YallaFinance.WebApi.Authorization;

public sealed class FunctionalityRequirement(IReadOnlyCollection<string> codes) : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> Codes { get; } = codes;
}
