using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Identifica o recebimento que efetivamente quitou uma cobrança; será populada na fase de confirmação financeira.
/// </summary>
public partial class PaymentSettlement
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ChargeId { get; set; }

    /// <summary>
    /// Recebimento escolhido para liquidar a cobrança; uma transação pode liquidar no máximo uma cobrança.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Instante UTC da liquidação local, distinto do horário bancário do recebimento.
    /// </summary>
    public DateTime SettledAt { get; set; }

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

    public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
