using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Customer cadastral address. One per tenant/customer initially; independent of payment/card billing data.
/// </summary>
public partial class CustomerAddress
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid CustomerId { get; set; }

    public string? CountryCode { get; set; }

    public string? PostalCode { get; set; }

    public string? Street { get; set; }

    public string? Number { get; set; }

    public bool? WithoutNumber { get; set; }

    public string? Complement { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    public string? StateCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
