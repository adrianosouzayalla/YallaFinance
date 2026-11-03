using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public enum PixReceivedQueryOutcome
{
    Completed,
    Rejected,
    Indeterminate
}
