using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class Customer
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>
    /// Identifier supplied by Yalla; do not infer it from the internal UUID.
    /// </summary>
    public string? ExternalId { get; set; }

    public string Name { get; set; } = null!;

    public string Document { get; set; } = null!;

    public string DocumentType { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

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

    public string? DocumentIssuingCountry { get; set; }

    public string? PhoneCountryCallingCode { get; set; }

    public string? PhoneNationalNumber { get; set; }

    /// <summary>
    /// Canonical document for lookup; preserves ASCII letters and leading zeros.
    /// </summary>
    public string? NormalizedDocument { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual CustomerAddress? CustomerAddress { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
