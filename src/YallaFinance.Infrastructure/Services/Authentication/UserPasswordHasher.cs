using Microsoft.AspNetCore.Identity;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Infrastructure.Repositories.Access;

namespace YallaFinance.Infrastructure.Services.Authentication;
public sealed class UserPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new();
    private readonly object identity = new();
    public string Hash(string password)
    {
        UserManagementValidation.Password(password);
        return hasher.HashPassword(identity, password);
    }
    public bool Verify(string? hash, string password)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password) || password.Length > 128) return false;
        try { return hasher.VerifyHashedPassword(identity, hash, password) != PasswordVerificationResult.Failed; }
        catch (Exception error) when (error is FormatException or ArgumentException or IndexOutOfRangeException) { return false; }
    }
}
