using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class Job
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Type { get; set; } = null!;

    /// <summary>
    /// Business correlation key; neither unique nor an idempotency key.
    /// </summary>
    public string Key { get; set; } = null!;

    public string Payload { get; set; } = null!;

    /// <summary>
    /// 0 Pending, 1 Processing, 2 Success, 3 Failed
    /// </summary>
    public int Status { get; set; }

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

    public virtual IntegrationOperation? IntegrationOperation { get; set; }

    public virtual ICollection<JobExecution> JobExecutions { get; set; } = new List<JobExecution>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
