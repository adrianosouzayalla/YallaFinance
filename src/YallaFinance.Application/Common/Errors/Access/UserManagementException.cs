using YallaFinance.Application;
namespace YallaFinance.Application.Common.Errors.Access;

public sealed class UserManagementException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
