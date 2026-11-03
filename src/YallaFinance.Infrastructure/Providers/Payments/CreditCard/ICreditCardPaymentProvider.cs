using YallaFinance.Infrastructure.Providers.Configuration;
using YallaFinance.Application.Services.Payments.CreditCard.Contracts;

namespace YallaFinance.Infrastructure.Providers.Payments.CreditCard;

// Technical boundary only. The caller owns eligibility, durable intent, concurrency and confirmation.
internal interface ICreditCardPaymentProvider : IProviderCapability
{
    Task<CardProviderResult<CardProviderPage<CardCustomer>>> FindCustomersAsync(string reference, int offset, int limit, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardCustomer>> CreateCustomerAsync(CreateCardCustomer customer, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardCharge>> CreateChargeAsync(CreateCardCharge charge, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardCharge>> PayChargeAsync(string chargeId, CardPaymentInput payment, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardCharge>> GetChargeAsync(string chargeId, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CreditCardChargeCancellation>> CancelChargeAsync(string chargeId, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardChargeStatus>> GetChargeStatusAsync(string chargeId, ProviderConnection connection, CancellationToken cancellationToken);
    Task<CardProviderResult<CardProviderPage<CardCharge>>> FindChargesAsync(string reference, int offset, int limit, ProviderConnection connection, CancellationToken cancellationToken);
}

internal enum CardProviderOutcome { Completed, Rejected, Indeterminate, ConfigurationError }
internal enum CardPaymentState
{
    Pending, UnderReview, Confirmed, Received, Authorized, Overdue,
    Refunded, RefundPending, ChargebackRequested, ChargebackDispute, AwaitingChargebackReversal, Unknown
}

// Completed means a valid API response, not that the card was charged. Inspect Value.State.
internal sealed record CardProviderResult<T>(CardProviderOutcome Outcome, T? Value, string? ErrorCode = null)
{
    public CreditCardProviderDiagnostic? Diagnostic { get; init; }
}
internal sealed record CardProviderPage<T>(IReadOnlyList<T> Items, bool HasMore, int Offset, int Limit);
internal sealed record CardCustomer(string Id, string? Reference, string? Document)
{
    public override string ToString() => nameof(CardCustomer);
}
internal sealed record CardCharge(string Id, string CustomerId, decimal Amount, string? Reference, CardPaymentState State, string ProviderStatus)
{
    public bool Deleted { get; init; }
    public string ProviderPayloadJson { get; init; } = "{}";
}
internal sealed record CardChargeStatus(string Id, CardPaymentState State, string ProviderStatus);
internal sealed record CreateCardCharge(string CustomerId, decimal Amount, DateOnly DueDate, string Reference, string? Description);
internal sealed record CreateCardCustomer(string Name, string Document, string? Email, string? Phone, string Reference)
{
    public override string ToString() => nameof(CreateCardCustomer);
}

// Transient input: never store in jobs/entities/cache or serialize for observability.
internal sealed record CardPaymentInput(
    string HolderName, string Number, string ExpiryMonth, string ExpiryYear, string SecurityCode,
    string PayerName, string Email, string Document, string PostalCode, string AddressNumber,
    string Phone, string? AddressComplement = null, string? MobilePhone = null)
{
    public override string ToString() => nameof(CardPaymentInput);
}
