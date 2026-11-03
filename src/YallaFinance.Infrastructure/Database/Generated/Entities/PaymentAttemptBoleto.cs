using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Generic boleto instrument data for a Yalla payment attempt; provider-specific HTTP payloads are not stored.
/// </summary>
public partial class PaymentAttemptBoleto
{
    public Guid PaymentAttemptId { get; set; }

    public Guid TenantId { get; set; }

    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Remote payment identifier returned by the configured provider; immutable after first assignment.
    /// </summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>
    /// Latest relevant payment status returned by the configured provider.
    /// </summary>
    public string? ProviderStatus { get; set; }

    /// <summary>
    /// Boleto digit line, mapped from the provider payment instrument response.
    /// </summary>
    public string? IdentificationField { get; set; }

    /// <summary>
    /// Boleto barcode, when returned by the provider.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Boleto number, when returned by the provider.
    /// </summary>
    public string? BankSlipNumber { get; set; }

    /// <summary>
    /// URL to the boleto document, when returned by the provider.
    /// </summary>
    public string? BankSlipUrl { get; set; }

    public string? InvoiceUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PaymentAttempt PaymentAttempt { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
