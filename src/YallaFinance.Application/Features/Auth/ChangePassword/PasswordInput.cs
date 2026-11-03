using YallaFinance.Application;
namespace YallaFinance.Application.Features.Auth.ChangePassword;

public sealed class PasswordInput
{
    public string NewPassword { get; init; } = "";
    public string CurrentPassword { get; init; } = "";
    public bool ConfirmGlobalImpact
    {
        get; init;
    }
}
