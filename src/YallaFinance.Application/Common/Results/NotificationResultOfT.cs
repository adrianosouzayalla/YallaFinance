using YallaFinance.Application;

namespace YallaFinance.Application.Common.Results;

public sealed class NotificationResult<T> : NotificationResult
{
    private NotificationResult(bool isSuccess, T? value, IReadOnlyCollection<NotificationError> errors)
        : base(isSuccess, errors) => Value = value;

    public T? Value
    {
        get;
    }

    public static NotificationResult<T> Success(T value) => new(true, value, Array.Empty<NotificationError>());

    public new static NotificationResult<T> Failure(params NotificationError[] errors) => new(
        false,
        default,
        errors);

    public new static NotificationResult<T> Failure(IEnumerable<NotificationError> errors) => new(
        false,
        default,
        errors.ToArray());
}
