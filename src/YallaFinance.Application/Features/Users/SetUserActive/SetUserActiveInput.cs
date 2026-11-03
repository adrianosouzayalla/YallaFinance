using YallaFinance.Application;
namespace YallaFinance.Application.Features.Users.SetUserActive;

public sealed record SetUserActiveInput(bool Active, bool ConfirmGlobalImpact = false);
