using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class Debt
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ContractId { get; set; }

    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly OccurrenceDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    /// <summary>
    /// Required debt source, such as Yalla Digital or Yalla Finance; identifies external debts together with tenant_id and external_id.
    /// </summary>
    public string Origin { get; set; } = null!;

    public string? ExternalId { get; set; }

    /// <summary>
    /// Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ChargeItem? ChargeItem { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ManualDebtRequest> ManualDebtRequests { get; set; } = new List<ManualDebtRequest>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
