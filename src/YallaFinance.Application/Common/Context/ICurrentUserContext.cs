using YallaFinance.Application;
namespace YallaFinance.Application.Common.Context;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Email
    {
        get;
    }
}
