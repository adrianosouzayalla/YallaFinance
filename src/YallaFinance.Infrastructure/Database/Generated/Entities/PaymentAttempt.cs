using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Execução de um método em uma conexão fixa, com referência bancária e controle de risco.
/// </summary>
public partial class PaymentAttempt
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
    /// Identificador público CSPRNG de 32 bytes em base64url sem padding; requer token da request na consulta.
    /// </summary>
    public string PublicId { get; set; } = null!;

    /// <summary>
    /// Método imutável e autorizado na request.
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Conexão externa do mesmo tenant; autoridade para credenciais e correlação bancária.
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Snapshot imutável do identificador do provedor, ex.: Sicoob; não substitui connection_id.
    /// </summary>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// Pending, Processing, AwaitingPayment, Succeeded, Failed, Expired, Canceled ou Unknown.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Chave interna estável da tentativa para fronteira bancária; cliques possuem recibos separados.
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// TXID único gerado antes da chamada ao provider e usado como referência Pix da tentativa.
    /// </summary>
    public string ProviderExternalId { get; set; } = null!;

    /// <summary>
    /// Validade bancária do Pix, independente da validade do link; desconhecida antes da emissão.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Reserva de risco por Charge; índice impede dois riscos abertos em qualquer request.
    /// </summary>
    public bool IssuanceBlocked { get; set; }

    /// <summary>
    /// Instante em que a conciliação provou ser seguro encerrar o risco.
    /// </summary>
    public DateTime? RiskClosedAt { get; set; }

    /// <summary>
    /// Evidência sanitizada/referência verificável de encerramento; obrigatória ao liberar issuance_blocked.
    /// </summary>
    public string? RiskClosureEvidence { get; set; }

    /// <summary>
    /// Código sanitizado de falha; não armazenar resposta bancária bruta.
    /// </summary>
    public string? FailureCode { get; set; }

    /// <summary>
    /// Mensagem operacional sanitizada, sem segredo ou dados completos do pagador.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Valor autorizado e capturado da cobrança na criação da tentativa; positivo e finito.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PaymentAttemptBoleto> PaymentAttemptBoletos { get; set; } = new List<PaymentAttemptBoleto>();

    public virtual ICollection<PaymentAttemptCard> PaymentAttemptCards { get; set; } = new List<PaymentAttemptCard>();

    public virtual ICollection<PaymentAttemptPix> PaymentAttemptPixes { get; set; } = new List<PaymentAttemptPix>();

    public virtual ICollection<PaymentCommandReceipt> PaymentCommandReceipts { get; set; } = new List<PaymentCommandReceipt>();

    public virtual PaymentMethodConnection PaymentMethodConnection { get; set; } = null!;

    public virtual PaymentRequest PaymentRequest { get; set; } = null!;

    public virtual PaymentRequestMethod PaymentRequestMethod { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
