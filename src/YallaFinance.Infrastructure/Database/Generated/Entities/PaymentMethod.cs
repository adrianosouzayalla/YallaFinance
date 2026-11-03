using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// One method per tenant/code; availability is controlled by active without duplicate inactive versions.
/// </summary>
public partial class PaymentMethod
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool Active { get; set; }

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

    /// <summary>
    /// Payment instrument: Pix, CreditCard or Boleto. NULL means not classified; independent of method code, name and provider.
    /// </summary>
    public string? InstrumentType { get; set; }

    /// <summary>
    /// Optional internal description for administrative selection; never used for routing or public checkout presentation.
    /// </summary>
    public string? InternalDescription { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PaymentMethodConnection? PaymentMethodConnection { get; set; }

    public virtual ICollection<PaymentRequestMethod> PaymentRequestMethods { get; set; } = new List<PaymentRequestMethod>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
