using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

/// <summary>
/// Dados textuais específicos do instrumento Pix, em relação 1:1 com a tentativa.
/// </summary>
public partial class PaymentAttemptPix
{
    /// <summary>
    /// Identificador da tentativa proprietária; também é a chave primária do detalhe Pix.
    /// </summary>
    public Guid PaymentAttemptId { get; set; }

    /// <summary>
    /// Tenant da tentativa; a FK composta impede associação entre tenants.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Pix Copia e Cola validado, apresentado também como origem do QR Code.
    /// </summary>
    public string? Brcode { get; set; }

    /// <summary>
    /// Localização textual do Pix retornada pelo provider, quando disponível.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Identificador opaco da localização Pix, preservado como texto.
    /// </summary>
    public string? LocationId { get; set; }

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

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual PaymentAttempt PaymentAttempt { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
