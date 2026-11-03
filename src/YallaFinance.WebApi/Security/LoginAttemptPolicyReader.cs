using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Auth.LoginAttemptPolicy;

namespace YallaFinance.WebApi.Security;

/// <summary>Reads the failed-login limit for the account identified by the login, from the company settings.</summary>
public interface ILoginAttemptPolicyReader
{
    Task<NotificationResult<LoginFailurePolicy?>> ReadAsync(string identifier, CancellationToken cancellationToken);
}

public sealed class MediatorLoginAttemptPolicyReader(ISender sender) : ILoginAttemptPolicyReader
{
    public Task<NotificationResult<LoginFailurePolicy?>> ReadAsync(string identifier, CancellationToken cancellationToken) =>
        sender.Send(new GetLoginAttemptPolicyQuery(identifier), cancellationToken);
}
