using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Card-specific attempt state; the parent attempt binds the payment request. Never stores PAN, CVV or card tokens.
/// </summary>
public partial class PaymentAttemptCard
{
    public Guid PaymentAttemptId { get; set; }

    public Guid TenantId { get; set; }

    public Guid ConnectionId { get; set; }

    public string? ProviderCustomerId { get; set; }

    public string? ProviderChargeId { get; set; }

    public string? ProviderStatus { get; set; }

    /// <summary>
    /// Durable intent recorded before paying. Once set, recovery queries the provider and never automatically pays again.
    /// </summary>
    public DateTime? PaymentDispatchedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PaymentAttempt PaymentAttempt { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
