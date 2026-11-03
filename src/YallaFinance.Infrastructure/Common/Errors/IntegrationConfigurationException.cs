namespace YallaFinance.Infrastructure.Common.Errors;

internal sealed class IntegrationConfigurationException(string code)
    : Exception("Financial integration configuration is invalid: " + code)
{
    public string Code { get; } = code;
}
