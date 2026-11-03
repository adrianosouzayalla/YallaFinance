using YallaFinance.Application;
namespace YallaFinance.Application.Features.Users.CreateUser;

public sealed record CreateUserInput(string Login, string Name, string Email, bool Active = true, Guid? ProfileId = null);
