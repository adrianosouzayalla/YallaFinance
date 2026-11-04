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
    /// <summary>True while the user must replace the password before using the system (feature 28, phase 3).</summary>
    public bool MustChangePassword { get; init; }
}
