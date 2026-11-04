using System;
using System.Collections.Generic;

namespace YallaFinance.Infrastructure.Database.Generated.Entities;

public partial class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// Global login normalized with lower(btrim(login)); textual codes retain leading zeros.
    /// </summary>
    public string Login { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>
    /// Restricted credential hash; NULL prevents local password authentication. Never store a default password.
    /// </summary>
    public string? PasswordHash { get; set; }

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
    /// Identidade operacional/tecnica. Nao concede permissoes. Bloqueio explicito de login sera implementado no backend.
    /// </summary>
    public bool IsOperational { get; set; }

    /// <summary>
    /// FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public bool MustChangePassword { get; set; }

    public virtual ICollection<AccessProfile> AccessProfileCreatedByNavigations { get; set; } = new List<AccessProfile>();

    public virtual ICollection<AccessProfileFunctionality> AccessProfileFunctionalityCreatedByNavigations { get; set; } = new List<AccessProfileFunctionality>();

    public virtual ICollection<AccessProfileFunctionality> AccessProfileFunctionalityUpdatedByNavigations { get; set; } = new List<AccessProfileFunctionality>();

    public virtual ICollection<AccessProfile> AccessProfileUpdatedByNavigations { get; set; } = new List<AccessProfile>();

    public virtual ICollection<Category> CategoryCreatedByNavigations { get; set; } = new List<Category>();

    public virtual ICollection<Category> CategoryUpdatedByNavigations { get; set; } = new List<Category>();

    public virtual ICollection<Charge> ChargeCreatedByNavigations { get; set; } = new List<Charge>();

    public virtual ICollection<ChargeItem> ChargeItemCreatedByNavigations { get; set; } = new List<ChargeItem>();

    public virtual ICollection<ChargeItem> ChargeItemUpdatedByNavigations { get; set; } = new List<ChargeItem>();

    public virtual ICollection<Charge> ChargeUpdatedByNavigations { get; set; } = new List<Charge>();

    public virtual ICollection<Contract> ContractCreatedByNavigations { get; set; } = new List<Contract>();

    public virtual ICollection<Contract> ContractUpdatedByNavigations { get; set; } = new List<Contract>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<CustomerAddress> CustomerAddressCreatedByNavigations { get; set; } = new List<CustomerAddress>();

    public virtual ICollection<CustomerAddress> CustomerAddressUpdatedByNavigations { get; set; } = new List<CustomerAddress>();

    public virtual ICollection<Customer> CustomerCreatedByNavigations { get; set; } = new List<Customer>();

    public virtual ICollection<Customer> CustomerUpdatedByNavigations { get; set; } = new List<Customer>();

    public virtual ICollection<Debt> DebtCreatedByNavigations { get; set; } = new List<Debt>();

    public virtual ICollection<Debt> DebtUpdatedByNavigations { get; set; } = new List<Debt>();

    public virtual ICollection<ExternalConnection> ExternalConnectionCreatedByNavigations { get; set; } = new List<ExternalConnection>();

    public virtual ICollection<ExternalConnectionMapping> ExternalConnectionMappingCreatedByNavigations { get; set; } = new List<ExternalConnectionMapping>();

    public virtual ICollection<ExternalConnectionMapping> ExternalConnectionMappingUpdatedByNavigations { get; set; } = new List<ExternalConnectionMapping>();

    public virtual ICollection<ExternalConnectionSetting> ExternalConnectionSettingCreatedByNavigations { get; set; } = new List<ExternalConnectionSetting>();

    public virtual ICollection<ExternalConnectionSetting> ExternalConnectionSettingUpdatedByNavigations { get; set; } = new List<ExternalConnectionSetting>();

    public virtual ICollection<ExternalConnection> ExternalConnectionUpdatedByNavigations { get; set; } = new List<ExternalConnection>();

    public virtual ICollection<Functionality> FunctionalityCreatedByNavigations { get; set; } = new List<Functionality>();

    public virtual ICollection<Functionality> FunctionalityUpdatedByNavigations { get; set; } = new List<Functionality>();

    public virtual ICollection<IntegrationOperation> IntegrationOperationCreatedByNavigations { get; set; } = new List<IntegrationOperation>();

    public virtual ICollection<IntegrationOperation> IntegrationOperationUpdatedByNavigations { get; set; } = new List<IntegrationOperation>();

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> InverseUpdatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Job> JobCreatedByNavigations { get; set; } = new List<Job>();

    public virtual ICollection<JobExecution> JobExecutionCreatedByNavigations { get; set; } = new List<JobExecution>();

    public virtual ICollection<JobExecution> JobExecutionUpdatedByNavigations { get; set; } = new List<JobExecution>();

    public virtual ICollection<Job> JobUpdatedByNavigations { get; set; } = new List<Job>();

    public virtual ICollection<ManualDebtRequest> ManualDebtRequestCreatedByNavigations { get; set; } = new List<ManualDebtRequest>();

    public virtual ICollection<ManualDebtRequest> ManualDebtRequestUpdatedByNavigations { get; set; } = new List<ManualDebtRequest>();

    public virtual ICollection<PaymentAttemptBoleto> PaymentAttemptBoletoCreatedByNavigations { get; set; } = new List<PaymentAttemptBoleto>();

    public virtual ICollection<PaymentAttemptBoleto> PaymentAttemptBoletoUpdatedByNavigations { get; set; } = new List<PaymentAttemptBoleto>();

    public virtual ICollection<PaymentAttemptCard> PaymentAttemptCardCreatedByNavigations { get; set; } = new List<PaymentAttemptCard>();

    public virtual ICollection<PaymentAttemptCard> PaymentAttemptCardUpdatedByNavigations { get; set; } = new List<PaymentAttemptCard>();

    public virtual ICollection<PaymentAttempt> PaymentAttemptCreatedByNavigations { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<PaymentAttemptPix> PaymentAttemptPixCreatedByNavigations { get; set; } = new List<PaymentAttemptPix>();

    public virtual ICollection<PaymentAttemptPix> PaymentAttemptPixUpdatedByNavigations { get; set; } = new List<PaymentAttemptPix>();

    public virtual ICollection<PaymentAttempt> PaymentAttemptUpdatedByNavigations { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<PaymentCommandReceipt> PaymentCommandReceiptCreatedByNavigations { get; set; } = new List<PaymentCommandReceipt>();

    public virtual ICollection<PaymentCommandReceipt> PaymentCommandReceiptUpdatedByNavigations { get; set; } = new List<PaymentCommandReceipt>();

    public virtual ICollection<PaymentMethodConnection> PaymentMethodConnectionCreatedByNavigations { get; set; } = new List<PaymentMethodConnection>();

    public virtual ICollection<PaymentMethodConnection> PaymentMethodConnectionUpdatedByNavigations { get; set; } = new List<PaymentMethodConnection>();

    public virtual ICollection<PaymentMethod> PaymentMethodCreatedByNavigations { get; set; } = new List<PaymentMethod>();

    public virtual ICollection<PaymentMethod> PaymentMethodUpdatedByNavigations { get; set; } = new List<PaymentMethod>();

    public virtual ICollection<PaymentRequest> PaymentRequestCreatedByNavigations { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<PaymentRequestMethod> PaymentRequestMethodCreatedByNavigations { get; set; } = new List<PaymentRequestMethod>();

    public virtual ICollection<PaymentRequestMethod> PaymentRequestMethodUpdatedByNavigations { get; set; } = new List<PaymentRequestMethod>();

    public virtual ICollection<PaymentRequest> PaymentRequestUpdatedByNavigations { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<PaymentSettlement> PaymentSettlementCreatedByNavigations { get; set; } = new List<PaymentSettlement>();

    public virtual ICollection<PaymentSettlement> PaymentSettlementUpdatedByNavigations { get; set; } = new List<PaymentSettlement>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionCreatedByNavigations { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionUpdatedByNavigations { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<TenantAccess> TenantAccessCreatedByNavigations { get; set; } = new List<TenantAccess>();

    public virtual ICollection<TenantAccess> TenantAccessUpdatedByNavigations { get; set; } = new List<TenantAccess>();

    public virtual ICollection<TenantAccess> TenantAccessUsers { get; set; } = new List<TenantAccess>();

    public virtual ICollection<Tenant> TenantCreatedByNavigations { get; set; } = new List<Tenant>();

    public virtual ICollection<TenantSetting> TenantSettingCreatedByNavigations { get; set; } = new List<TenantSetting>();

    public virtual ICollection<TenantSetting> TenantSettingUpdatedByNavigations { get; set; } = new List<TenantSetting>();

    public virtual ICollection<Tenant> TenantUpdatedByNavigations { get; set; } = new List<Tenant>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserSession> UserSessionCreatedByNavigations { get; set; } = new List<UserSession>();

    public virtual ICollection<UserSession> UserSessionUpdatedByNavigations { get; set; } = new List<UserSession>();

    public virtual ICollection<UserSession> UserSessionUsers { get; set; } = new List<UserSession>();
}
