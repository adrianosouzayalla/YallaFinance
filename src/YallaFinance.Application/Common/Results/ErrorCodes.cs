using YallaFinance.Application;

namespace YallaFinance.Application.Common.Results;

public static class ErrorCodes
{
    public const string Validation = "Validation";
    public const string NotFound = "NotFound";
    public const string Conflict = "Conflict";
    public const string Unauthorized = "Unauthorized";
    public const string Forbidden = "Forbidden";
    public const string BusinessRule = "BusinessRule";
    public const string ServiceUnavailable = "ServiceUnavailable";
    public const string IntegrationOperationConflict = "IntegrationOperationConflict";
}
