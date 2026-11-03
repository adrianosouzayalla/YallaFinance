using YallaFinance.Infrastructure.Common.Errors;

namespace YallaFinance.Infrastructure.Providers;

internal sealed class ProviderResolver<TCapability>(IEnumerable<TCapability> implementations)
    : IProviderResolver<TCapability>
    where TCapability : IProviderCapability
{
    private readonly IReadOnlyDictionary<string, TCapability> providers = CreateProviderMap(implementations);

    public TCapability Resolve(string provider)
        => providers.TryGetValue(provider, out var implementation)
            ? implementation
            : throw new IntegrationConfigurationException("ProviderNotRegistered");

    public bool CanResolve(string provider) => providers.ContainsKey(provider);

    private static IReadOnlyDictionary<string, TCapability> CreateProviderMap(IEnumerable<TCapability> implementations)
    {
        var registeredProviders = implementations.ToArray();
        if (registeredProviders.Any(implementation => string.IsNullOrWhiteSpace(implementation.Provider))
            || registeredProviders.GroupBy(implementation => implementation.Provider, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
            throw new IntegrationConfigurationException("DuplicateOrInvalidProvider");

        return registeredProviders.ToDictionary(
            implementation => implementation.Provider,
            StringComparer.Ordinal);
    }
}
