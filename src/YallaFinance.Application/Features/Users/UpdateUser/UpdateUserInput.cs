using YallaFinance.Application;
namespace YallaFinance.Application.Features.Users.UpdateUser;

public sealed record UpdateUserInput(string Name, string Email, Guid? ProfileId = null);
