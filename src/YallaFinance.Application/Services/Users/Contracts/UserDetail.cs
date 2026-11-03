using YallaFinance.Application;
namespace YallaFinance.Application.Services.Users.Contracts;

public sealed record UserDetail(Guid Id, string Login, string Name, string? Email, bool Active,
    bool AccessActive, Guid? ProfileId, string? ProfileName);
