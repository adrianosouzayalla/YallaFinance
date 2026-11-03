using YallaFinance.Application;
namespace YallaFinance.Application.Services.Financial.Contracts;

// Only controlled operation names, route templates and technical classifications belong here.
// Provider bodies, exception messages, credentials and request data must never be persisted.
public sealed record FinancialProviderDiagnostic(
    string Operation,
    string Method,
    string Endpoint,
    int? HttpStatusCode = null,
    string? ExceptionType = null,
    string? TransportError = null,
    string? ProviderErrorCode = null,
    string? ProviderErrorMessage = null);
