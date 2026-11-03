using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application;

namespace YallaFinance.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(
            validationContext,
            cancellationToken)));
        var validationErrors = validationResults
            .SelectMany(validationResult => validationResult.Errors)
            .Where(error => error is not null)
            .Select(error => new NotificationError(
                ErrorCodes.Validation,
                error.ErrorMessage,
                error.PropertyName))
            .ToArray();

        if (validationErrors.Length == 0)
            return await next();
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(NotificationResult<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            return (TResponse)typeof(NotificationResult<>).MakeGenericType(valueType)
                .GetMethod(
                    nameof(NotificationResult<string>.Failure),
                    new[] { typeof(IEnumerable<NotificationError>) })!
                .Invoke(null, new object[] { validationErrors })!;
        }

        if (typeof(TResponse) == typeof(NotificationResult))
            return (TResponse)(object)NotificationResult.Failure(validationErrors);
        throw new ValidationException(validationResults.SelectMany(validationResult => validationResult.Errors));
    }
}
