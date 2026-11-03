using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class ChargeItem
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ChargeId { get; set; }

    /// <summary>
    /// Required for both Selected and Standalone charges; each debt may be charged only once in this phase.
    /// </summary>
    public Guid DebtId { get; set; }

    /// <summary>
    /// Full debt amount at charge creation; aggregate and debt-value equality are validated by the transactional write operation.
    /// </summary>
    public decimal Amount { get; set; }

    public int Ordinal { get; set; }

    /// <summary>
    /// Generic identifier of the resolved financial provider; NULL before resolution. Not restricted to Nibo.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Confirmed external posting identifier for this charge item; NULL until creation or reconciliation confirms the posting. The charge item UUID is the stable external reference.
    /// </summary>
    public string? ProviderExternalId { get; set; }

    /// <summary>
    /// Opaque external cost-center identifier selected for this item; independent of provider_external_id, which identifies the financial posting.
    /// </summary>
    public string? ExternalCostCenterId { get; set; }

    /// <summary>
    /// Cost-center display name captured when the item was created; existing charge items may have no cost center.
    /// </summary>
    public string? CostCenterName { get; set; }

    /// <summary>
    /// FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual Charge Charge { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Debt Debt { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
