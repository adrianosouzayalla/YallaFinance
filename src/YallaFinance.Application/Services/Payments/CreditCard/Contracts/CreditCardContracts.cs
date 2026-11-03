namespace YallaFinance.Application.Services.Payments.CreditCard.Contracts;

public enum CreditCardOperationOutcome { Completed, Rejected, Indeterminate, ConfigurationError }
public enum CreditCardPaymentState
{
    Pending, UnderReview, Confirmed, Received, Authorized, Overdue,
    Refunded, RefundPending, ChargebackRequested, ChargebackDispute, AwaitingChargebackReversal, Unknown
}

// Completed describes the operation response; Value.State determines the financial state.
public sealed record CreditCardResult<T>(CreditCardOperationOutcome Outcome, T? Value, string? ErrorCode = null)
{
    public CreditCardProviderDiagnostic? Diagnostic { get; init; }
}
public sealed record CreditCardProviderError(string Code, string? Description)
{
    public override string ToString() => nameof(CreditCardProviderError);
}
public sealed record CreditCardProviderDiagnostic(int? HttpStatusCode, IReadOnlyList<CreditCardProviderError> Errors)
{
    public override string ToString() => nameof(CreditCardProviderDiagnostic);
}
public sealed record CreditCardPage<T>(IReadOnlyList<T> Items, bool HasMore, int Offset, int Limit);
public sealed record CreditCardConnectionContext(Guid TenantId, Guid ConnectionId, string ExpectedProvider);
public sealed record CreditCardCustomer(string Id, string? Reference, string? Document)
{
    public override string ToString() => nameof(CreditCardCustomer);
}
public sealed record CreditCardCharge(string Id, string CustomerId, decimal Amount, string? Reference, CreditCardPaymentState State, string ProviderStatus)
{
    public bool Deleted { get; init; }
    public string ProviderPayloadJson { get; init; } = "{}";

    public override string ToString() => nameof(CreditCardCharge);
}
public sealed record CreditCardChargeCancellation(string Id, bool Deleted);
public sealed record CreditCardCustomerQuery(CreditCardConnectionContext Context, string Reference, int Offset, int Limit);
public sealed record CreditCardChargeSearch(CreditCardConnectionContext Context, string Reference, int Offset, int Limit);
public sealed record CreditCardChargeQuery(CreditCardConnectionContext Context, string ChargeId);
public sealed record CreateCreditCardCustomerData(CreditCardConnectionContext Context, Guid OperationId,
    string Reference, string Name, string Document, string? Email = null, string? Phone = null)
{
    public override string ToString() => nameof(CreateCreditCardCustomerData);
}
public sealed record CreateCreditCardChargeData(CreditCardConnectionContext Context, Guid OperationId,
    string CustomerId, decimal Amount, DateOnly DueDate, string Reference, string? Description = null);
public sealed record PayCreditCardChargeData(CreditCardConnectionContext Context, Guid OperationId,
    string ChargeId, string ExpectedCustomerId, decimal ExpectedAmount, string ExpectedReference, CreditCardPaymentInput Payment)
{
    public override string ToString() => nameof(PayCreditCardChargeData);
}

// Transient input. Never persist in entities/jobs/cache or serialize for diagnostics.
public sealed record CreditCardPaymentInput(
    string HolderName, string Number, string ExpiryMonth, string ExpiryYear, string SecurityCode,
    string PayerName, string Email, string Document, string PostalCode, string AddressNumber,
    string Phone, string? AddressComplement = null, string? MobilePhone = null)
{
    public override string ToString() => nameof(CreditCardPaymentInput);
}
