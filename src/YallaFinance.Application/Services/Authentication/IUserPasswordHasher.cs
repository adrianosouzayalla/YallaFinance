using YallaFinance.Application;
namespace YallaFinance.Application.Services.Authentication;

public interface IUserPasswordHasher
{
    string Hash(string password);
    bool Verify(string? hash, string password);
}
