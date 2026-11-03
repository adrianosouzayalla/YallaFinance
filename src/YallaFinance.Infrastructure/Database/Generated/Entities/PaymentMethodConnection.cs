using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Unique assignment of a payment method to its provider connection.
/// </summary>
public partial class PaymentMethodConnection
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
    /// Método cadastrado em finance.payment_method.
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Conexão externa do mesmo tenant; autoridade para credenciais e correlação bancária.
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Autoriza o vínculo para novas emissões; não desliga conciliação de instrumentos existentes.
    /// </summary>
    public bool Enabled { get; set; }

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

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ExternalConnection ExternalConnection { get; set; } = null!;

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
