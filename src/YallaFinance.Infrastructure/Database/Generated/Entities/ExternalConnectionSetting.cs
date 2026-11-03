using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class ExternalConnectionSetting
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ConnectionId { get; set; }

    public string Group { get; set; } = null!;

    public string Key { get; set; } = null!;

    /// <summary>
    /// Connection configuration, including provider authentication token settings.
    /// </summary>
    public string Value { get; set; } = null!;

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

    public virtual User? UpdatedByNavigation { get; set; }
}
