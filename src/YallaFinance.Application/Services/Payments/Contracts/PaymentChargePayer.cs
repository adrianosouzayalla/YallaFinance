namespace YallaFinance.Application.Services.Payments.Contracts;

public sealed record PaymentChargePayer(string TaxId, string Name, Guid? CustomerId = null);

public sealed record PaymentChargeCreationContext(DateOnly DueDate, PaymentChargePayer? Payer);
