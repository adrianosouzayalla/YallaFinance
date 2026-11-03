using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Recibo de idempotência de comandos síncronos/públicos; separa chaves de cliques da identidade da tentativa.
/// </summary>
public partial class PaymentCommandReceipt
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
    /// Cobrança existente à qual pertence o registro; não cria outra cobrança.
    /// </summary>
    public Guid ChargeId { get; set; }

    /// <summary>
    /// Solicitação proprietária do registro.
    /// </summary>
    public Guid PaymentRequestId { get; set; }

    /// <summary>
    /// Tentativa de resultado; obrigatória em CreatePaymentAttempt e NULL em CreatePaymentRequest.
    /// </summary>
    public Guid? PaymentAttemptId { get; set; }

    /// <summary>
    /// CreatePaymentRequest ou CreatePaymentAttempt.
    /// </summary>
    public string Operation { get; set; } = null!;

    /// <summary>
    /// Chave do header do comando; escopo determinado pela operação e recurso pai.
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// SHA-256 lowercase do comando canônico; distingue replay de chave usada com payload diferente.
    /// </summary>
    public string RequestHash { get; set; } = null!;

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

    public virtual PaymentAttempt? PaymentAttempt { get; set; }

    public virtual PaymentRequest PaymentRequest { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
