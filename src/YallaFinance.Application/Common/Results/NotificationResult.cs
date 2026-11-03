using YallaFinance.Application;

namespace YallaFinance.Application.Common.Results;

public class NotificationResult
{
    protected NotificationResult(bool isSuccess, IReadOnlyCollection<NotificationError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess
    {
        get;
    }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyCollection<NotificationError> Errors
    {
        get;
    }

    public static NotificationResult Success() => new(true, Array.Empty<NotificationError>());

    public static NotificationResult Failure(params NotificationError[] errors) => new(false, errors);

    public static NotificationResult Failure(IEnumerable<NotificationError> errors) => new(
        false,
        errors.ToArray());
}
