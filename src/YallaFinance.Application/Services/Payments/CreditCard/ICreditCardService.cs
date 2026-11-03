using YallaFinance.Application.Services.Payments.CreditCard.Contracts;

namespace YallaFinance.Application.Services.Payments.CreditCard;

// Integration operations only. The caller owns authorization, durable intent,
// remote ID storage and concurrency before paying an existing charge.
public interface ICreditCardService
{
    Task<CreditCardResult<CreditCardPage<CreditCardCustomer>>> FindCustomersAsync(CreditCardCustomerQuery query, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardCustomer>> CreateCustomerAsync(CreateCreditCardCustomerData data, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardCharge>> CreateChargeAsync(CreateCreditCardChargeData data, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardCharge>> PayChargeAsync(PayCreditCardChargeData data, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardCharge>> GetChargeAsync(CreditCardChargeQuery query, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardChargeCancellation>> CancelChargeAsync(CreditCardChargeQuery query, CancellationToken cancellationToken);
    Task<CreditCardResult<CreditCardPage<CreditCardCharge>>> FindChargesAsync(CreditCardChargeSearch query, CancellationToken cancellationToken);
}
