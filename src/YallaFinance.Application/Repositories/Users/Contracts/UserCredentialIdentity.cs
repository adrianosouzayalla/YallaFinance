using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Users.Contracts;

public sealed class UserCredentialIdentity
{
    public Guid Id
    {
        get; init;
    }
    public string Name { get; init; } = "";
    public string? PasswordHash
    {
        get; init;
    }
}
