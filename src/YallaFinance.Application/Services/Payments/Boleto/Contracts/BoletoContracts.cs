namespace YallaFinance.Application.Services.Payments.Boleto.Contracts;

public enum BoletoOperationOutcome { Completed, Rejected, Indeterminate, ConfigurationError, NotFound }

public enum BoletoPaymentState { Pending, Confirmed, Received, Overdue, Cancelled, Refunded, Unknown }

public sealed record BoletoOperationResult<T>(BoletoOperationOutcome Outcome, T? Value, string? ErrorCode = null)
{
    public string? FailureMessage { get; init; }
}

public sealed record BoletoConnectionContext(Guid TenantId, Guid ConnectionId, string ExpectedProvider);

public sealed record CreateBoletoChargeData(
    BoletoConnectionContext Context,
    Guid OperationId,
    string ProviderReference,
    decimal Amount,
    DateOnly DueDate,
    BoletoPayer Payer,
    string? Description = null);

public sealed record BoletoChargeQuery(
    BoletoConnectionContext Context,
    Guid OperationId,
    string ProviderReference,
    string? ProviderPaymentId = null);

public sealed record BoletoPayer(Guid? CustomerId, string Name, string TaxId);

public sealed record BoletoCharge(
    string ProviderPaymentId,
    decimal Amount,
    string ProviderReference,
    BoletoPaymentState State,
    string ProviderStatus,
    string? IdentificationField,
    string? Barcode,
    string? BankSlipNumber,
    string? BankSlipUrl,
    string? InvoiceUrl,
    string ProviderPayloadJson)
{
    public bool Deleted { get; init; }
}

public sealed record PublicBoletoDetails(
    string? IdentificationField,
    string? Barcode,
    string? BankSlipNumber,
    string? BankSlipUrl,
    string? InvoiceUrl);
