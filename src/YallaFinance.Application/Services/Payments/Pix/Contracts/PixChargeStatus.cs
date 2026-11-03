using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public enum PixChargeStatus
{
    Active, Completed, RemovedByReceiver, RemovedByProvider
}
