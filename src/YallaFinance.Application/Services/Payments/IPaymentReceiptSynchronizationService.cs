namespace YallaFinance.Application.Services.Payments;

public interface IPaymentReceiptSynchronizationService
{
    Task SynchronizeAsync(CancellationToken cancellationToken);
}
