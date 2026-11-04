using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class PasswordChangeTicket
{
    public Guid Id { get; set; }

    public string TicketHash { get; set; } = null!;

    public string Login { get; set; } = null!;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
