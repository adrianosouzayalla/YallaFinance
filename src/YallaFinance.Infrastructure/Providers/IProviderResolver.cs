namespace YallaFinance.Infrastructure.Providers;

internal interface IProviderResolver<TCapability>
    where TCapability : IProviderCapability
{
    TCapability Resolve(string provider);
    bool CanResolve(string provider);
}
