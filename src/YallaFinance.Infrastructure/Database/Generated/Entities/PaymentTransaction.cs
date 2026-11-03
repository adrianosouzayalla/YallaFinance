using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Registro idempotente de cada Pix recebido pelo provider para uma tentativa do Yalla; não confirma quitação nem baixa.
/// </summary>
public partial class PaymentTransaction
{
    /// <summary>
    /// Identificador interno do recebimento no Yalla; é a chave usada pelas telas e relações internas.
    /// </summary>
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ChargeId { get; set; }

    public Guid PaymentAttemptId { get; set; }

    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Identificador bancário da transferência; referência externa usada para deduplicar consultas.
    /// </summary>
    public string? EndToEndId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// PIX: receipt time reported by provider. Card: time at which Yalla first verified a confirmed payment.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Objeto JSON individual do Pix recebido, preservado para auditoria; não inclui resposta de autenticação ou paginação.
    /// </summary>
    public string ProviderPayload { get; set; } = null!;

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

    /// <summary>
    /// Non-PIX provider payment identity. Mutually exclusive with end_to_end_id; scoped to tenant and connection.
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PaymentAttempt PaymentAttempt { get; set; } = null!;

    public virtual ICollection<PaymentSettlement> PaymentSettlements { get; set; } = new List<PaymentSettlement>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
