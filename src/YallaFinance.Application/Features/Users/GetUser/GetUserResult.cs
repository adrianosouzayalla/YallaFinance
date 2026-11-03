using YallaFinance.Application.Services.Users.Contracts;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Users.GetUser;

public sealed record GetUserResult(UserDetail User);
