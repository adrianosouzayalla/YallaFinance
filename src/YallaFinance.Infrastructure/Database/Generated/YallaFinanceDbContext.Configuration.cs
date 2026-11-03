using Microsoft.EntityFrameworkCore;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Database.Generated;

public partial class YallaFinanceDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Shadow properties before the owner's scaffold; binds to the generated CLR
        // properties of the same names afterwards, without duplicate declarations.
        modelBuilder.Entity<ChargeItem>().Property<string>("ExternalCostCenterId")
            .HasColumnName("external_cost_center_id").HasColumnType("text").IsRequired(false);
        modelBuilder.Entity<ChargeItem>().Property<string>("CostCenterName")
            .HasColumnName("cost_center_name").HasColumnType("text").IsRequired(false);
        // Database-first compatibility until/after the next scaffold; no edits in Generated.
        // The scaffold interprets the filtered default index as a global unique tenant FK.
        // A tenant can have multiple connections; only enabled defaults are unique.
        modelBuilder.Entity<Tenant>().Ignore("ExternalConnection");
        modelBuilder.Entity<ExternalConnection>()
            .HasOne(connection => connection.Tenant)
            .WithMany()
            .HasForeignKey(connection => connection.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_external_connection_tenant");

        // The filtered Active index does not make the Charge FK globally unique.
        // Keep this override outside Generated so re-scaffolding preserves 1:N history.
        modelBuilder.Entity<Charge>().Ignore("PaymentRequest");
        modelBuilder.Entity<PaymentRequest>()
            .HasOne(request => request.Charge)
            .WithMany()
            .HasPrincipalKey(charge => new { charge.TenantId, charge.Id })
            .HasForeignKey(request => new { request.TenantId, request.ChargeId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_request_charge");

        // The database primary key on payment_attempt_pix is payment_attempt_id,
        // but reverse scaffolding models its composite tenant-aware FK as a collection.
        // Keep EF aligned with the database's one-detail-per-attempt cardinality.
        modelBuilder.Entity<PaymentAttempt>().Ignore(attempt => attempt.PaymentAttemptPixes);
        modelBuilder.Entity<PaymentAttemptPix>()
            .HasOne(detail => detail.PaymentAttempt)
            .WithOne()
            .HasPrincipalKey<PaymentAttempt>(attempt => new { attempt.TenantId, attempt.Id })
            .HasForeignKey<PaymentAttemptPix>(detail => new { detail.TenantId, detail.PaymentAttemptId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_attempt_pix_attempt");

        modelBuilder.Entity<PaymentAttempt>().Ignore(attempt => attempt.PaymentAttemptBoletos);
        modelBuilder.Entity<PaymentAttemptBoleto>()
            .HasOne(detail => detail.PaymentAttempt)
            .WithOne()
            .HasPrincipalKey<PaymentAttempt>(attempt => new { attempt.TenantId, attempt.Id, attempt.ConnectionId })
            .HasForeignKey<PaymentAttemptBoleto>(detail => new { detail.TenantId, detail.PaymentAttemptId, detail.ConnectionId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payment_attempt_boleto_attempt");
    }
}
