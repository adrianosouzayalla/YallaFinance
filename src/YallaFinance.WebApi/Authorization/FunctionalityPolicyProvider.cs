using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace YallaFinance.WebApi.Authorization;

public sealed class FunctionalityPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        string? value = null;
        string separator = ",";
        if (policyName.StartsWith("Functionality:", StringComparison.Ordinal))
            value = policyName["Functionality:".Length..];
        else if (policyName.StartsWith("FunctionalityAnyOf:", StringComparison.Ordinal))
        {
            value = policyName["FunctionalityAnyOf:".Length..];
            separator = "|";
        }
        if (value is null) return base.GetPolicyAsync(policyName);

        var codes = value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(code => code.ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (codes.Length == 0 || codes.Any(code => code.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '_')))
            return Task.FromResult<AuthorizationPolicy?>(null);
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
            .AddRequirements(new FunctionalityRequirement(codes)).Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
