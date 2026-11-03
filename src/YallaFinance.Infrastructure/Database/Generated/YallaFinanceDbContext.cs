using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Database.Generated;

public partial class YallaFinanceDbContext : DbContext
{
    public YallaFinanceDbContext(DbContextOptions<YallaFinanceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessProfile> AccessProfiles { get; set; }

    public virtual DbSet<AccessProfileFunctionality> AccessProfileFunctionalities { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Charge> Charges { get; set; }

    public virtual DbSet<ChargeItem> ChargeItems { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerAddress> CustomerAddresses { get; set; }

    public virtual DbSet<DatabaseVersion> DatabaseVersions { get; set; }

    public virtual DbSet<Debt> Debts { get; set; }

    public virtual DbSet<ExternalConnection> ExternalConnections { get; set; }

    public virtual DbSet<ExternalConnectionMapping> ExternalConnectionMappings { get; set; }

    public virtual DbSet<ExternalConnectionSetting> ExternalConnectionSettings { get; set; }

    public virtual DbSet<Functionality> Functionalities { get; set; }

    public virtual DbSet<IntegrationOperation> IntegrationOperations { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobExecution> JobExecutions { get; set; }

    public virtual DbSet<ManualDebtRequest> ManualDebtRequests { get; set; }

    public virtual DbSet<PaymentAttempt> PaymentAttempts { get; set; }

    public virtual DbSet<PaymentAttemptBoleto> PaymentAttemptBoletos { get; set; }

    public virtual DbSet<PaymentAttemptCard> PaymentAttemptCards { get; set; }

    public virtual DbSet<PaymentAttemptPix> PaymentAttemptPixes { get; set; }

    public virtual DbSet<PaymentCommandReceipt> PaymentCommandReceipts { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PaymentMethodConnection> PaymentMethodConnections { get; set; }

    public virtual DbSet<PaymentRequest> PaymentRequests { get; set; }

    public virtual DbSet<PaymentRequestMethod> PaymentRequestMethods { get; set; }

    public virtual DbSet<PaymentSettlement> PaymentSettlements { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<TenantAccess> TenantAccesses { get; set; }

    public virtual DbSet<TenantSetting> TenantSettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_access_profile");

            entity.ToTable("access_profile", "core");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "uk_access_profile_code").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_access_profile_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AccessProfileCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.AccessProfiles)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AccessProfileUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_updated_by");
        });

        modelBuilder.Entity<AccessProfileFunctionality>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.ProfileId, e.FunctionalityId }).HasName("pk_access_profile_functionality");

            entity.ToTable("access_profile_functionality", "core", tb => tb.HasComment("Explicit per-tenant grants; ADMIN has no authorization bypass."));

            entity.HasIndex(e => e.FunctionalityId, "ix_access_profile_functionality_functionality");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.FunctionalityId).HasColumnName("functionality_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AccessProfileFunctionalityCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_functionality_created_by");

            entity.HasOne(d => d.Functionality).WithMany(p => p.AccessProfileFunctionalities)
                .HasForeignKey(d => d.FunctionalityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_functionality_functionality");

            entity.HasOne(d => d.Tenant).WithMany(p => p.AccessProfileFunctionalities)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_functionality_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AccessProfileFunctionalityUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_functionality_updated_by");

            entity.HasOne(d => d.AccessProfile).WithMany(p => p.AccessProfileFunctionalities)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ProfileId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_access_profile_functionality_profile");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_category");

            entity.ToTable("category", "finance");

            entity.HasIndex(e => new { e.TenantId, e.Code }, "uk_category_tenant_code").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_category_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CategoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_category_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Categories)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_category_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CategoryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_category_updated_by");
        });

        modelBuilder.Entity<Charge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_charge");

            entity.ToTable("charge", "finance", tb => tb.HasComment("Customer is resolved through charge_item, debt and contract; no customer_id is stored here."));

            entity.HasIndex(e => e.Number, "uk_charge_number").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_charge_tenant_id").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }, "uk_charge_tenant_idempotency_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Number)
                .ValueGeneratedOnAdd()
                .HasComment("Positive globally unique charge number generated by charge_number_seq. Display as tenant code-number; UUID remains the identity for relationships. Gaps are expected.")
                .HasColumnName("number");
            entity.Property(e => e.RequestHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnName("request_hash");
            entity.Property(e => e.Source)
                .HasMaxLength(16)
                .HasColumnName("source");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ChargeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Charges)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ChargeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_updated_by");
        });

        modelBuilder.Entity<ChargeItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_charge_item");

            entity.ToTable("charge_item", "finance");

            entity.HasIndex(e => new { e.TenantId, e.ChargeId, e.Ordinal }, "uk_charge_item_tenant_charge_ordinal").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.DebtId }, "uk_charge_item_tenant_debt").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_charge_item_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasComment("Full debt amount at charge creation; aggregate and debt-value equality are validated by the transactional write operation.")
                .HasColumnName("amount");
            entity.Property(e => e.ChargeId).HasColumnName("charge_id");
            entity.Property(e => e.CostCenterName)
                .HasComment("Cost-center display name captured when the item was created; existing charge items may have no cost center.")
                .HasColumnName("cost_center_name");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.DebtId)
                .HasComment("Required for both Selected and Standalone charges; each debt may be charged only once in this phase.")
                .HasColumnName("debt_id");
            entity.Property(e => e.ExternalCostCenterId)
                .HasComment("Opaque external cost-center identifier selected for this item; independent of provider_external_id, which identifies the financial posting.")
                .HasColumnName("external_cost_center_id");
            entity.Property(e => e.Ordinal).HasColumnName("ordinal");
            entity.Property(e => e.Provider)
                .HasComment("Generic identifier of the resolved financial provider; NULL before resolution. Not restricted to Nibo.")
                .HasColumnName("provider");
            entity.Property(e => e.ProviderExternalId)
                .HasComment("Confirmed external posting identifier for this charge item; NULL until creation or reconciliation confirms the posting. The charge item UUID is the stable external reference.")
                .HasColumnName("provider_external_id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ChargeItemCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_item_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ChargeItemUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_item_updated_by");

            entity.HasOne(d => d.Charge).WithMany(p => p.ChargeItems)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_item_charge");

            entity.HasOne(d => d.Debt).WithOne(p => p.ChargeItem)
                .HasPrincipalKey<Debt>(p => new { p.TenantId, p.Id })
                .HasForeignKey<ChargeItem>(d => new { d.TenantId, d.DebtId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_charge_item_debt");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_contract");

            entity.ToTable("contract", "finance");

            entity.HasIndex(e => new { e.TenantId, e.CustomerId, e.Id }, "ix_contract_tenant_customer_id");

            entity.HasIndex(e => new { e.TenantId, e.Number }, "ix_contract_tenant_number");

            entity.HasIndex(e => new { e.TenantId, e.ExternalId }, "uk_contract_tenant_external_id")
                .IsUnique()
                .HasFilter("(external_id IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_contract_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.Fleet).HasColumnName("fleet");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Plate).HasColumnName("plate");
            entity.Property(e => e.Product).HasColumnName("product");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContractCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_contract_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_contract_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ContractUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_contract_updated_by");

            entity.HasOne(d => d.Customer).WithMany(p => p.Contracts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.CustomerId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_contract_customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_customer");

            entity.ToTable("customer", "finance");

            entity.HasIndex(e => new { e.TenantId, e.NormalizedDocument }, "ix_customer_tenant_document");

            entity.HasIndex(e => new { e.TenantId, e.Name }, "ix_customer_tenant_name");

            entity.HasIndex(e => new { e.TenantId, e.ExternalId }, "uk_customer_tenant_external_id")
                .IsUnique()
                .HasFilter("(external_id IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_customer_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Document).HasColumnName("document");
            entity.Property(e => e.DocumentIssuingCountry)
                .HasMaxLength(2)
                .HasColumnName("document_issuing_country");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(4)
                .HasColumnName("document_type");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.ExternalId)
                .HasComment("Identifier supplied by Yalla; do not infer it from the internal UUID.")
                .HasColumnName("external_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.NormalizedDocument)
                .HasComputedColumnSql("regexp_replace(upper(document), '[^A-Z0-9]'::text, ''::text, 'g'::text)", true)
                .HasComment("Canonical document for lookup; preserves ASCII letters and leading zeros.")
                .HasColumnName("normalized_document");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.PhoneCountryCallingCode)
                .HasMaxLength(3)
                .HasColumnName("phone_country_calling_code");
            entity.Property(e => e.PhoneNationalNumber)
                .HasMaxLength(14)
                .HasColumnName("phone_national_number");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CustomerCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Customers)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CustomerUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_updated_by");
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_customer_address");

            entity.ToTable("customer_address", "finance", tb => tb.HasComment("Customer cadastral address. One per tenant/customer initially; independent of payment/card billing data."));

            entity.HasIndex(e => new { e.TenantId, e.CustomerId }, "uk_customer_address_tenant_customer").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Complement).HasColumnName("complement");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.District).HasColumnName("district");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.StateCode)
                .HasMaxLength(2)
                .HasColumnName("state_code");
            entity.Property(e => e.Street).HasColumnName("street");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.WithoutNumber).HasColumnName("without_number");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CustomerAddressCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_address_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.CustomerAddresses)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_address_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CustomerAddressUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_address_updated_by");

            entity.HasOne(d => d.Customer).WithOne(p => p.CustomerAddress)
                .HasPrincipalKey<Customer>(p => new { p.TenantId, p.Id })
                .HasForeignKey<CustomerAddress>(d => new { d.TenantId, d.CustomerId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_address_customer");
        });

        modelBuilder.Entity<DatabaseVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_database_version");

            entity.ToTable("database_version", "core");

            entity.HasIndex(e => e.ScriptName, "uk_database_version_script_name").IsUnique();

            entity.HasIndex(e => e.Version, "uk_database_version_version").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AppliedAtUtc)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("applied_at_utc");
            entity.Property(e => e.AppliedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("CURRENT_USER")
                .HasColumnName("applied_by");
            entity.Property(e => e.ChecksumSha256)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnName("checksum_sha256");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.ScriptName)
                .HasMaxLength(255)
                .HasColumnName("script_name");
            entity.Property(e => e.Version)
                .HasMaxLength(20)
                .HasColumnName("version");
        });

        modelBuilder.Entity<Debt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_debt");

            entity.ToTable("debt", "finance");

            entity.HasIndex(e => new { e.TenantId, e.CategoryId, e.Id }, "ix_debt_tenant_category_id");

            entity.HasIndex(e => new { e.TenantId, e.ContractId, e.Id }, "ix_debt_tenant_contract_id");

            entity.HasIndex(e => new { e.TenantId, e.Status, e.Id }, "ix_debt_tenant_status_id");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_debt_tenant_id").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Origin, e.ExternalId }, "uk_debt_tenant_origin_external_id")
                .IsUnique()
                .HasFilter("(external_id IS NOT NULL)");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.OccurrenceDate).HasColumnName("occurrence_date");
            entity.Property(e => e.Origin)
                .HasComment("Required debt source, such as Yalla Digital or Yalla Finance; identifies external debts together with tenant_id and external_id.")
                .HasColumnName("origin");
            entity.Property(e => e.Status)
                .HasMaxLength(16)
                .HasDefaultValueSql("'Open'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.DebtCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_debt_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Debts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_debt_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.DebtUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_debt_updated_by");

            entity.HasOne(d => d.Category).WithMany(p => p.Debts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.CategoryId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_debt_category");

            entity.HasOne(d => d.Contract).WithMany(p => p.Debts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ContractId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_debt_contract");
        });

        modelBuilder.Entity<ExternalConnection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_external_connection");

            entity.ToTable("external_connection", "core");

            entity.HasIndex(e => e.TenantId, "uk_external_connection_system_active")
                .IsUnique()
                .HasFilter("(enabled AND (\"group\" = 'FinancialSystem'::text))");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_external_connection_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.Group).HasColumnName("group");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ExternalConnectionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_created_by");

            entity.HasOne(d => d.Tenant).WithOne(p => p.ExternalConnection)
                .HasForeignKey<ExternalConnection>(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ExternalConnectionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_updated_by");
        });

        modelBuilder.Entity<ExternalConnectionMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_external_connection_mapping");

            entity.ToTable("external_connection_mapping", "core");

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.MappingType, e.InternalValue }, "uk_external_connection_mapping_identity").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ExternalValue).HasColumnName("external_value");
            entity.Property(e => e.InternalValue).HasColumnName("internal_value");
            entity.Property(e => e.MappingType).HasColumnName("mapping_type");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ExternalConnectionMappingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_mapping_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ExternalConnectionMappingUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_mapping_updated_by");
        });

        modelBuilder.Entity<ExternalConnectionSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_external_connection_setting");

            entity.ToTable("external_connection_setting", "core");

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.Group, e.Key }, "uk_external_connection_setting_identity").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Group).HasColumnName("group");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");
            entity.Property(e => e.Value)
                .HasComment("Connection configuration, including provider authentication token settings.")
                .HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ExternalConnectionSettingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_setting_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ExternalConnectionSettingUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_setting_updated_by");

            entity.HasOne(d => d.ExternalConnection).WithMany(p => p.ExternalConnectionSettings)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_external_connection_setting_connection");
        });

        modelBuilder.Entity<Functionality>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_functionality");

            entity.ToTable("functionality", "core");

            entity.HasIndex(e => e.Code, "uk_functionality_code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasComment("User-friendly explanation of the functionality shown when assigning functionality grants to an access profile.")
                .HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FunctionalityCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_functionality_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.FunctionalityUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_functionality_updated_by");
        });

        modelBuilder.Entity<IntegrationOperation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_integration_operation");

            entity.ToTable("integration_operation", "core", tb => tb.HasComment("Public receipt and request idempotency; execution state and history remain in job and job_execution."));

            entity.HasIndex(e => new { e.TenantId, e.CreatedAt, e.Id }, "ix_integration_operation_tenant_created");

            entity.HasIndex(e => new { e.TenantId, e.Type, e.ExternalId }, "uk_integration_operation_identity").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.JobId }, "uk_integration_operation_job").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.RequestFingerprint)
                .HasMaxLength(64)
                .HasColumnName("request_fingerprint");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(100)
                .HasColumnName("resource_type");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.IntegrationOperationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_integration_operation_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.IntegrationOperations)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_integration_operation_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.IntegrationOperationUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_integration_operation_updated_by");

            entity.HasOne(d => d.Job).WithOne(p => p.IntegrationOperation)
                .HasPrincipalKey<Job>(p => new { p.TenantId, p.Id })
                .HasForeignKey<IntegrationOperation>(d => new { d.TenantId, d.JobId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_integration_operation_job");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_job");

            entity.ToTable("job", "core");

            entity.HasIndex(e => new { e.CreatedAt, e.Id }, "ix_job_pending").HasFilter("(status = 0)");

            entity.HasIndex(e => new { e.TenantId, e.Key, e.CreatedAt, e.Id }, "ix_job_tenant_key");

            entity.HasIndex(e => new { e.TenantId, e.Type, e.Key, e.CreatedAt, e.Id }, "ix_job_tenant_type_key");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_job_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Key)
                .HasMaxLength(250)
                .HasComment("Business correlation key; neither unique nor an idempotency key.")
                .HasColumnName("key");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Status)
                .HasComment("0 Pending, 1 Processing, 2 Success, 3 Failed")
                .HasColumnName("status");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.JobCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.JobUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_updated_by");
        });

        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_job_execution");

            entity.ToTable("job_execution", "core");

            entity.HasIndex(e => new { e.TenantId, e.JobId, e.StartedAt, e.Id }, "ix_job_execution_tenant_job");

            entity.HasIndex(e => e.JobId, "uk_job_execution_open")
                .IsUnique()
                .HasFilter("(finished_at IS NULL)");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Details)
                .HasColumnType("jsonb")
                .HasColumnName("details");
            entity.Property(e => e.Error)
                .HasMaxLength(1000)
                .HasColumnName("error");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.Result)
                .HasColumnType("jsonb")
                .HasColumnName("result");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.JobExecutionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_execution_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.JobExecutionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_execution_updated_by");

            entity.HasOne(d => d.Job).WithMany(p => p.JobExecutions)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.JobId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_job_execution_job");
        });

        modelBuilder.Entity<ManualDebtRequest>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.IdempotencyKey }).HasName("pk_manual_debt_request");

            entity.ToTable("manual_debt_request", "finance", tb => tb.HasComment("Durable idempotency receipt for manual debt creation; no payload or job state."));

            entity.HasIndex(e => new { e.TenantId, e.DebtId }, "ix_manual_debt_request_debt");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.DebtId).HasColumnName("debt_id");
            entity.Property(e => e.RequestFingerprint)
                .HasMaxLength(64)
                .HasColumnName("request_fingerprint");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ManualDebtRequestCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_manual_debt_request_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ManualDebtRequestUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_manual_debt_request_updated_by");

            entity.HasOne(d => d.Debt).WithMany(p => p.ManualDebtRequests)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.DebtId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_manual_debt_request_debt");
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_attempt");

            entity.ToTable("payment_attempt", "finance", tb => tb.HasComment("Execução de um método em uma conexão fixa, com referência bancária e controle de risco."));

            entity.HasIndex(e => new { e.UpdatedAt, e.TenantId, e.Id }, "ix_payment_attempt_reconciliation").HasFilter("issuance_blocked");

            entity.HasIndex(e => new { e.TenantId, e.PaymentRequestId, e.CreatedAt, e.Id }, "ix_payment_attempt_request_history").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.TenantId, e.ChargeId }, "uk_payment_attempt_blocking_charge")
                .IsUnique()
                .HasFilter("issuance_blocked");

            entity.HasIndex(e => new { e.TenantId, e.Id, e.ConnectionId }, "uk_payment_attempt_card_identity").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id, e.ChargeId, e.ConnectionId }, "uk_payment_attempt_connection_identity").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }, "uk_payment_attempt_internal_key").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.ProviderExternalId }, "uk_payment_attempt_provider_external_id").IsUnique();

            entity.HasIndex(e => e.PublicId, "uk_payment_attempt_public_id").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id, e.PaymentRequestId, e.ChargeId }, "uk_payment_attempt_request_identity").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_attempt_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno UUID gerado pela aplicação; nunca é token público.")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasComment("Valor autorizado e capturado da cobrança na criação da tentativa; positivo e finito.")
                .HasColumnName("amount");
            entity.Property(e => e.ChargeId)
                .HasComment("Cobrança existente à qual pertence o registro; não cria outra cobrança.")
                .HasColumnName("charge_id");
            entity.Property(e => e.ConnectionId)
                .HasComment("Conexão externa do mesmo tenant; autoridade para credenciais e correlação bancária.")
                .HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ExpiresAt)
                .HasComment("Validade bancária do Pix, independente da validade do link; desconhecida antes da emissão.")
                .HasColumnName("expires_at");
            entity.Property(e => e.FailureCode)
                .HasComment("Código sanitizado de falha; não armazenar resposta bancária bruta.")
                .HasColumnName("failure_code");
            entity.Property(e => e.FailureMessage)
                .HasMaxLength(1000)
                .HasComment("Mensagem operacional sanitizada, sem segredo ou dados completos do pagador.")
                .HasColumnName("failure_message");
            entity.Property(e => e.IdempotencyKey)
                .HasComment("Chave interna estável da tentativa para fronteira bancária; cliques possuem recibos separados.")
                .HasColumnName("idempotency_key");
            entity.Property(e => e.IssuanceBlocked)
                .HasDefaultValue(true)
                .HasComment("Reserva de risco por Charge; índice impede dois riscos abertos em qualquer request.")
                .HasColumnName("issuance_blocked");
            entity.Property(e => e.PaymentMethodId)
                .HasComment("Método imutável e autorizado na request.")
                .HasColumnName("payment_method_id");
            entity.Property(e => e.PaymentRequestId)
                .HasComment("Solicitação proprietária do registro.")
                .HasColumnName("payment_request_id");
            entity.Property(e => e.Provider)
                .HasComment("Snapshot imutável do identificador do provedor, ex.: Sicoob; não substitui connection_id.")
                .HasColumnName("provider");
            entity.Property(e => e.ProviderExternalId)
                .HasComment("TXID único gerado antes da chamada ao provider e usado como referência Pix da tentativa.")
                .HasColumnName("provider_external_id");
            entity.Property(e => e.PublicId)
                .HasMaxLength(43)
                .HasComment("Identificador público CSPRNG de 32 bytes em base64url sem padding; requer token da request na consulta.")
                .HasColumnName("public_id");
            entity.Property(e => e.RiskClosedAt)
                .HasComment("Instante em que a conciliação provou ser seguro encerrar o risco.")
                .HasColumnName("risk_closed_at");
            entity.Property(e => e.RiskClosureEvidence)
                .HasComment("Evidência sanitizada/referência verificável de encerramento; obrigatória ao liberar issuance_blocked.")
                .HasColumnName("risk_closure_evidence");
            entity.Property(e => e.Status)
                .HasMaxLength(24)
                .HasDefaultValueSql("'Pending'::character varying")
                .HasComment("Pending, Processing, AwaitingPayment, Succeeded, Failed, Expired, Canceled ou Unknown.")
                .HasColumnName("status");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant proprietário; participa de todas as relações financeiras.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentAttemptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentAttempts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentAttemptUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_updated_by");

            entity.HasOne(d => d.PaymentMethodConnection).WithMany(p => p.PaymentAttempts)
                .HasPrincipalKey(p => new { p.TenantId, p.PaymentMethodId, p.ConnectionId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentMethodId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_method_connection");

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.PaymentAttempts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ChargeId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentRequestId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_request_charge");

            entity.HasOne(d => d.PaymentRequestMethod).WithMany(p => p.PaymentAttempts)
                .HasPrincipalKey(p => new { p.TenantId, p.PaymentRequestId, p.PaymentMethodId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentRequestId, d.PaymentMethodId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_authorized_method");
        });

        modelBuilder.Entity<PaymentAttemptBoleto>(entity =>
        {
            entity.HasKey(e => e.PaymentAttemptId).HasName("payment_attempt_boleto_pkey");

            entity.ToTable("payment_attempt_boleto", "finance", tb => tb.HasComment("Generic boleto instrument data for a Yalla payment attempt; provider-specific HTTP payloads are not stored."));

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.ProviderPaymentId }, "uk_payment_attempt_boleto_remote_payment")
                .IsUnique()
                .HasFilter("(provider_payment_id IS NOT NULL)");

            entity.Property(e => e.PaymentAttemptId)
                .ValueGeneratedNever()
                .HasColumnName("payment_attempt_id");
            entity.Property(e => e.BankSlipNumber)
                .HasComment("Boleto number, when returned by the provider.")
                .HasColumnName("bank_slip_number");
            entity.Property(e => e.BankSlipUrl)
                .HasComment("URL to the boleto document, when returned by the provider.")
                .HasColumnName("bank_slip_url");
            entity.Property(e => e.Barcode)
                .HasComment("Boleto barcode, when returned by the provider.")
                .HasColumnName("barcode");
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IdentificationField)
                .HasComment("Boleto digit line, mapped from the provider payment instrument response.")
                .HasColumnName("identification_field");
            entity.Property(e => e.InvoiceUrl).HasColumnName("invoice_url");
            entity.Property(e => e.ProviderPaymentId)
                .HasComment("Remote payment identifier returned by the configured provider; immutable after first assignment.")
                .HasColumnName("provider_payment_id");
            entity.Property(e => e.ProviderStatus)
                .HasComment("Latest relevant payment status returned by the configured provider.")
                .HasColumnName("provider_status");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentAttemptBoletoCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_boleto_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentAttemptBoletoUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_boleto_updated_by");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentAttemptBoletos)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ConnectionId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentAttemptId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_boleto_attempt");
        });

        modelBuilder.Entity<PaymentAttemptCard>(entity =>
        {
            entity.HasKey(e => e.PaymentAttemptId).HasName("payment_attempt_card_pkey");

            entity.ToTable("payment_attempt_card", "finance", tb => tb.HasComment("Card-specific attempt state; the parent attempt binds the payment request. Never stores PAN, CVV or card tokens."));

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.ProviderChargeId }, "uk_payment_attempt_card_remote_charge")
                .IsUnique()
                .HasFilter("(provider_charge_id IS NOT NULL)");

            entity.Property(e => e.PaymentAttemptId)
                .ValueGeneratedNever()
                .HasColumnName("payment_attempt_id");
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PaymentDispatchedAt)
                .HasComment("Durable intent recorded before paying. Once set, recovery queries the provider and never automatically pays again.")
                .HasColumnName("payment_dispatched_at");
            entity.Property(e => e.ProviderChargeId).HasColumnName("provider_charge_id");
            entity.Property(e => e.ProviderCustomerId).HasColumnName("provider_customer_id");
            entity.Property(e => e.ProviderStatus).HasColumnName("provider_status");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentAttemptCardCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_card_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentAttemptCardUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_card_updated_by");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentAttemptCards)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ConnectionId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentAttemptId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_card_attempt");
        });

        modelBuilder.Entity<PaymentAttemptPix>(entity =>
        {
            entity.HasKey(e => e.PaymentAttemptId).HasName("pk_payment_attempt_pix");

            entity.ToTable("payment_attempt_pix", "finance", tb => tb.HasComment("Dados textuais específicos do instrumento Pix, em relação 1:1 com a tentativa."));

            entity.Property(e => e.PaymentAttemptId)
                .ValueGeneratedNever()
                .HasComment("Identificador da tentativa proprietária; também é a chave primária do detalhe Pix.")
                .HasColumnName("payment_attempt_id");
            entity.Property(e => e.Brcode)
                .HasComment("Pix Copia e Cola validado, apresentado também como origem do QR Code.")
                .HasColumnName("brcode");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Location)
                .HasComment("Localização textual do Pix retornada pelo provider, quando disponível.")
                .HasColumnName("location");
            entity.Property(e => e.LocationId)
                .HasComment("Identificador opaco da localização Pix, preservado como texto.")
                .HasColumnName("location_id");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant da tentativa; a FK composta impede associação entre tenants.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentAttemptPixCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_pix_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentAttemptPixUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_pix_updated_by");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentAttemptPixes)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.PaymentAttemptId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempt_pix_attempt");
        });

        modelBuilder.Entity<PaymentCommandReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_command_receipt");

            entity.ToTable("payment_command_receipt", "finance", tb => tb.HasComment("Recibo de idempotência de comandos síncronos/públicos; separa chaves de cliques da identidade da tentativa."));

            entity.HasIndex(e => new { e.TenantId, e.PaymentRequestId, e.IdempotencyKey }, "uk_payment_command_receipt_create_attempt")
                .IsUnique()
                .HasFilter("((operation)::text = 'CreatePaymentAttempt'::text)");

            entity.HasIndex(e => new { e.TenantId, e.ChargeId, e.IdempotencyKey }, "uk_payment_command_receipt_create_request")
                .IsUnique()
                .HasFilter("((operation)::text = 'CreatePaymentRequest'::text)");

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_command_receipt_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno UUID gerado pela aplicação; nunca é token público.")
                .HasColumnName("id");
            entity.Property(e => e.ChargeId)
                .HasComment("Cobrança existente à qual pertence o registro; não cria outra cobrança.")
                .HasColumnName("charge_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.IdempotencyKey)
                .HasComment("Chave do header do comando; escopo determinado pela operação e recurso pai.")
                .HasColumnName("idempotency_key");
            entity.Property(e => e.Operation)
                .HasMaxLength(32)
                .HasComment("CreatePaymentRequest ou CreatePaymentAttempt.")
                .HasColumnName("operation");
            entity.Property(e => e.PaymentAttemptId)
                .HasComment("Tentativa de resultado; obrigatória em CreatePaymentAttempt e NULL em CreatePaymentRequest.")
                .HasColumnName("payment_attempt_id");
            entity.Property(e => e.PaymentRequestId)
                .HasComment("Solicitação proprietária do registro.")
                .HasColumnName("payment_request_id");
            entity.Property(e => e.RequestHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasComment("SHA-256 lowercase do comando canônico; distingue replay de chave usada com payload diferente.")
                .HasColumnName("request_hash");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant proprietário; participa de todas as relações financeiras.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentCommandReceiptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_command_receipt_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentCommandReceipts)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_command_receipt_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentCommandReceiptUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_command_receipt_updated_by");

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.PaymentCommandReceipts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ChargeId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentRequestId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_command_receipt_request");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentCommandReceipts)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.PaymentRequestId, p.ChargeId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentAttemptId, d.PaymentRequestId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_command_receipt_attempt");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_method");

            entity.ToTable("payment_method", "finance", tb => tb.HasComment("One method per tenant/code; availability is controlled by active without duplicate inactive versions."));

            entity.HasIndex(e => new { e.TenantId, e.Code }, "uk_payment_method_tenant_code").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_method_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.InstrumentType)
                .HasComment("Payment instrument: Pix, CreditCard or Boleto. NULL means not classified; independent of method code, name and provider.")
                .HasColumnName("instrument_type");
            entity.Property(e => e.InternalDescription)
                .HasComment("Optional internal description for administrative selection; never used for routing or public checkout presentation.")
                .HasColumnName("internal_description");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentMethodCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentMethods)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentMethodUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_updated_by");
        });

        modelBuilder.Entity<PaymentMethodConnection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_method_connection");

            entity.ToTable("payment_method_connection", "finance", tb => tb.HasComment("Unique assignment of a payment method to its provider connection."));

            entity.HasIndex(e => new { e.TenantId, e.PaymentMethodId, e.ConnectionId }, "uk_payment_method_connection_binding").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.PaymentMethodId }, "uk_payment_method_connection_payment_method").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_method_connection_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno UUID gerado pela aplicação; nunca é token público.")
                .HasColumnName("id");
            entity.Property(e => e.ConnectionId)
                .HasComment("Conexão externa do mesmo tenant; autoridade para credenciais e correlação bancária.")
                .HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Enabled)
                .HasDefaultValue(true)
                .HasComment("Autoriza o vínculo para novas emissões; não desliga conciliação de instrumentos existentes.")
                .HasColumnName("enabled");
            entity.Property(e => e.PaymentMethodId)
                .HasComment("Método cadastrado em finance.payment_method.")
                .HasColumnName("payment_method_id");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant proprietário; participa de todas as relações financeiras.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentMethodConnectionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_connection_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentMethodConnections)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_connection_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentMethodConnectionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_connection_updated_by");

            entity.HasOne(d => d.ExternalConnection).WithMany(p => p.PaymentMethodConnections)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_connection_connection");

            entity.HasOne(d => d.PaymentMethod).WithOne(p => p.PaymentMethodConnection)
                .HasPrincipalKey<PaymentMethod>(p => new { p.TenantId, p.Id })
                .HasForeignKey<PaymentMethodConnection>(d => new { d.TenantId, d.PaymentMethodId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_method_connection_method");
        });

        modelBuilder.Entity<PaymentRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_request");

            entity.ToTable("payment_request", "finance", tb => tb.HasComment("Oferta de pagamento e ciclo de vida do link; não duplica valor, cliente, vencimento ou contrato."));

            entity.HasIndex(e => new { e.TenantId, e.ChargeId, e.CreatedAt, e.Id }, "ix_payment_request_charge_history").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.ExpiresAt, e.TenantId, e.Id }, "ix_payment_request_expiration").HasFilter("((status)::text = 'Active'::text)");

            entity.HasIndex(e => new { e.TenantId, e.ChargeId }, "uk_payment_request_active_charge")
                .IsUnique()
                .HasFilter("((status)::text = 'Active'::text)");

            entity.HasIndex(e => new { e.TenantId, e.Id, e.ChargeId }, "uk_payment_request_charge_identity").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_request_tenant_id").IsUnique();

            entity.HasIndex(e => e.TokenHash, "uk_payment_request_token_hash").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno UUID gerado pela aplicação; nunca é token público.")
                .HasColumnName("id");
            entity.Property(e => e.CanceledAt)
                .HasComment("Instante de cancelamento do link pelo operador.")
                .HasColumnName("canceled_at");
            entity.Property(e => e.CanceledBy)
                .HasComment("Identidade do operador que cancelou; obrigatória em Canceled.")
                .HasColumnName("canceled_by");
            entity.Property(e => e.ChargeId)
                .HasComment("Cobrança existente à qual pertence o registro; não cria outra cobrança.")
                .HasColumnName("charge_id");
            entity.Property(e => e.CompletedAt)
                .HasComment("Quando o link Active foi encerrado pela liquidação da Charge.")
                .HasColumnName("completed_at");
            entity.Property(e => e.CompletionReason)
                .HasMaxLength(32)
                .HasComment("RequestPayment ou ChargeSettledElsewhere; só em Completed.")
                .HasColumnName("completion_reason");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ExpiredAt)
                .HasComment("Quando a aplicação persistiu a expiração; pode ocorrer depois de expires_at.")
                .HasColumnName("expired_at");
            entity.Property(e => e.ExpiresAt)
                .HasComment("Expiração do link; validada pelo relógio do servidor, independentemente do status persistido.")
                .HasColumnName("expires_at");
            entity.Property(e => e.Note)
                .HasComment("Nota operacional opcional; não expor no checkout.")
                .HasColumnName("note");
            entity.Property(e => e.Status)
                .HasMaxLength(24)
                .HasDefaultValueSql("'Active'::character varying")
                .HasComment("Active, Expired, Canceled ou Completed; ciclo do link, não estado do dinheiro.")
                .HasColumnName("status");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant proprietário; participa de todas as relações financeiras.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasComment("Unique opaque code generated by the application and used to locate the payment request from its public link.")
                .HasColumnName("token_hash");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentRequestCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentRequests)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentRequestUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_updated_by");

            entity.HasOne(d => d.Charge).WithOne(p => p.PaymentRequest)
                .HasPrincipalKey<Charge>(p => new { p.TenantId, p.Id })
                .HasForeignKey<PaymentRequest>(d => new { d.TenantId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_charge");
        });

        modelBuilder.Entity<PaymentRequestMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_request_method");

            entity.ToTable("payment_request_method", "finance", tb => tb.HasComment("Conjunto de métodos autorizado na publicação; não seleciona provedor."));

            entity.HasIndex(e => new { e.TenantId, e.PaymentRequestId, e.PaymentMethodId }, "uk_payment_request_method_authorization").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_request_method_tenant_id").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno UUID gerado pela aplicação; nunca é token público.")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.PaymentMethodId)
                .HasComment("Método autorizado; tentativa precisa pertencer a esse conjunto.")
                .HasColumnName("payment_method_id");
            entity.Property(e => e.PaymentRequestId)
                .HasComment("Solicitação proprietária do registro.")
                .HasColumnName("payment_request_id");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant proprietário; participa de todas as relações financeiras.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentRequestMethodCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_method_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentRequestMethods)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_method_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentRequestMethodUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_method_updated_by");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.PaymentRequestMethods)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.PaymentMethodId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_method_method");

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.PaymentRequestMethods)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.PaymentRequestId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_request_method_request");
        });

        modelBuilder.Entity<PaymentSettlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_settlement");

            entity.ToTable("payment_settlement", "finance", tb => tb.HasComment("Identifica o recebimento que efetivamente quitou uma cobrança; será populada na fase de confirmação financeira."));

            entity.HasIndex(e => new { e.TenantId, e.SettledAt, e.Id }, "ix_payment_settlement_period").IsDescending(false, true, false);

            entity.HasIndex(e => new { e.TenantId, e.ChargeId }, "uk_payment_settlement_charge").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_settlement_tenant_id").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.PaymentTransactionId }, "uk_payment_settlement_transaction").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ChargeId).HasColumnName("charge_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.PaymentTransactionId)
                .HasComment("Recebimento escolhido para liquidar a cobrança; uma transação pode liquidar no máximo uma cobrança.")
                .HasColumnName("payment_transaction_id");
            entity.Property(e => e.SettledAt)
                .HasComment("Instante UTC da liquidação local, distinto do horário bancário do recebimento.")
                .HasColumnName("settled_at");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentSettlementCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_settlement_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentSettlements)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_settlement_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentSettlementUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_settlement_updated_by");

            entity.HasOne(d => d.PaymentTransaction).WithMany(p => p.PaymentSettlements)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ChargeId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentTransactionId, d.ChargeId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_settlement_transaction_charge");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_payment_transaction");

            entity.ToTable("payment_transaction", "finance", tb => tb.HasComment("Registro idempotente de cada Pix recebido pelo provider para uma tentativa do Yalla; não confirma quitação nem baixa."));

            entity.HasIndex(e => new { e.TenantId, e.PaymentAttemptId, e.ReceivedAt, e.Id }, "ix_payment_transaction_attempt_history").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.TenantId, e.ChargeId, e.ReceivedAt, e.Id }, "ix_payment_transaction_charge_history").IsDescending(false, false, true, false);

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.ProviderTransactionId }, "uk_payment_transaction_provider_receipt")
                .IsUnique()
                .HasFilter("(provider_transaction_id IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.ConnectionId, e.EndToEndId }, "uk_payment_transaction_sicoob_receipt").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "uk_payment_transaction_tenant_id").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id, e.ChargeId }, "uk_payment_transaction_tenant_id_charge").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Identificador interno do recebimento no Yalla; é a chave usada pelas telas e relações internas.")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.ChargeId).HasColumnName("charge_id");
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.EndToEndId)
                .HasMaxLength(64)
                .HasComment("Identificador bancário da transferência; referência externa usada para deduplicar consultas.")
                .HasColumnName("end_to_end_id");
            entity.Property(e => e.PaymentAttemptId).HasColumnName("payment_attempt_id");
            entity.Property(e => e.ProviderPayload)
                .HasComment("Objeto JSON individual do Pix recebido, preservado para auditoria; não inclui resposta de autenticação ou paginação.")
                .HasColumnType("jsonb")
                .HasColumnName("provider_payload");
            entity.Property(e => e.ProviderTransactionId)
                .HasComment("Non-PIX provider payment identity. Mutually exclusive with end_to_end_id; scoped to tenant and connection.")
                .HasColumnName("provider_transaction_id");
            entity.Property(e => e.ReceivedAt)
                .HasComment("PIX: receipt time reported by provider. Card: time at which Yalla first verified a confirmed payment.")
                .HasColumnName("received_at");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentTransactionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_transaction_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_transaction_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PaymentTransactionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_transaction_updated_by");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentTransactions)
                .HasPrincipalKey(p => new { p.TenantId, p.Id, p.ChargeId, p.ConnectionId })
                .HasForeignKey(d => new { d.TenantId, d.PaymentAttemptId, d.ChargeId, d.ConnectionId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_transaction_attempt");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_tenant");

            entity.ToTable("tenant", "core");

            entity.HasIndex(e => e.Code, "uk_tenant_code").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code)
                .ValueGeneratedOnAdd()
                .HasComment("Positive globally unique tenant code generated by tenant_code_seq. Stable application reference; UUID remains the primary key.")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TenantCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TenantUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_updated_by");
        });

        modelBuilder.Entity<TenantAccess>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_tenant_access");

            entity.ToTable("tenant_access", "core");

            entity.HasIndex(e => new { e.UserEmail, e.TenantId }, "ix_tenant_access_email").HasFilter("active");

            entity.HasIndex(e => new { e.TenantId, e.ProfileId }, "ix_tenant_access_profile");

            entity.HasIndex(e => new { e.UserId, e.TenantId }, "ix_tenant_access_user");

            entity.HasIndex(e => new { e.TenantId, e.UserEmail }, "uk_tenant_access_email").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.UserId }, "uk_tenant_access_user").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");
            entity.Property(e => e.UserEmail)
                .HasComment("Compatibility identifier for current email-based login; new authorization uses user_id.")
                .HasColumnName("user_email");
            entity.Property(e => e.UserId)
                .HasComment("Required local identity; legacy email-only links were removed after provisioning replacement administrator access.")
                .HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TenantAccessCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_access_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.TenantAccesses)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_access_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TenantAccessUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_access_updated_by");

            entity.HasOne(d => d.User).WithMany(p => p.TenantAccessUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_access_user");

            entity.HasOne(d => d.AccessProfile).WithMany(p => p.TenantAccesses)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.ProfileId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_access_profile");
        });

        modelBuilder.Entity<TenantSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_tenant_setting");

            entity.ToTable("tenant_setting", "core", tb => tb.HasComment("Tenant-owned parameters. Every company stores its own values; no global/default fallback."));

            entity.HasIndex(e => new { e.TenantId, e.Group, e.Key }, "uk_tenant_setting_scope").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasComment("Internal UUID generated by the application.")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.Group)
                .HasMaxLength(100)
                .HasComment("Owning module, for example Payments; case-sensitive canonical identifier.")
                .HasColumnName("group");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .HasComment("Canonical parameter name, unique within tenant and group.")
                .HasColumnName("key");
            entity.Property(e => e.TenantId)
                .HasComment("Tenant owning the setting; obtained from authenticated context.")
                .HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");
            entity.Property(e => e.Value)
                .HasComment("Textual representation of a typed non-secret parameter; never encryption keys or provider credentials.")
                .HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TenantSettingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_setting_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.TenantSettings)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_setting_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TenantSettingUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_tenant_setting_updated_by");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_user");

            entity.ToTable("user", "core");

            entity.HasIndex(e => e.Login, "uk_user_login").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.IsOperational)
                .HasComment("Identidade operacional/tecnica. Nao concede permissoes. Bloqueio explicito de login sera implementado no backend.")
                .HasColumnName("is_operational");
            entity.Property(e => e.Login)
                .HasComment("Global login normalized with lower(btrim(login)); textual codes retain leading zeros.")
                .HasColumnName("login");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasComment("Restricted credential hash; NULL prevents local password authentication. Never store a default password.")
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_created_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.InverseUpdatedByNavigation)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_updated_by");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_user_session");

            entity.ToTable("user_session", "core", tb => tb.HasComment("Persisted interactive session. Runtime must validate active user/tenant/access and expiration/revocation on every protected request."));

            entity.HasIndex(e => new { e.TenantId, e.UserId, e.ExpiresAt }, "ix_user_session_unrevoked_access").HasFilter("(revoked_at IS NULL)");

            entity.HasIndex(e => e.UserId, "ix_user_session_user");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasComment("Criacao UTC, imutavel no runtime. Data historica preservada quando conhecida; ausencias preenchidas na Feature 22/Fase 1.")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK core.user. Legado atribuido administrativamente ao ADMIN na Feature 22/Fase 1; nao comprova autoria original.")
                .HasColumnName("created_by");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.UpdatedAt)
                .HasComment("Ultima alteracao UTC; NULL na criacao. Datas legadas podem coincidir com a criacao pelo padrao anterior.")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK core.user; NULL sem alteracao. Legado com data de alteracao atribuido administrativamente ao ADMIN.")
                .HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserSessionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_session_created_by");

            entity.HasOne(d => d.Tenant).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_session_tenant");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UserSessionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_session_updated_by");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessionUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_session_user");

            entity.HasOne(d => d.TenantAccess).WithMany(p => p.UserSessions)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.UserId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_session_access");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
