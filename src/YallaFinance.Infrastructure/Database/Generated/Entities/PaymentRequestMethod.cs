using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Conjunto de métodos autorizado na publicação; não seleciona provedor.
/// </summary>
public partial class PaymentRequestMethod
{
    /// <summary>
    /// Identificador interno UUID gerado pela aplicação; nunca é token público.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant proprietário; participa de todas as relações financeiras.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Solicitação proprietária do registro.
    /// </summary>
    public Guid PaymentRequestId { get; set; }

    /// <summary>
    /// Método autorizado; tentativa precisa pertencer a esse conjunto.
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual PaymentRequest PaymentRequest { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
