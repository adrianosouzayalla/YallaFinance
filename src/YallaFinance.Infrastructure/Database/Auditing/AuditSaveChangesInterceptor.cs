using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.Infrastructure.Database.Auditing;

public sealed class AuditSaveChangesInterceptor(CurrentExecutionActorContext actor, TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditProperties = ["CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context) Stamp(context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context) Stamp(context);
        return ValueTask.FromResult(result);
    }

    internal void Stamp(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        AuditWriteMetadata? metadata = null;
        foreach (var entry in context.ChangeTracker.Entries().ToArray())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)
                || (entry.Metadata.GetSchema() == "core" && entry.Metadata.GetTableName() == "database_version"))
                continue;
            foreach (var property in AuditProperties)
                if (entry.Metadata.FindProperty(property) is null)
                    throw new InvalidOperationException("A persistent application entity lacks required audit metadata.");

            if (entry.State == EntityState.Modified)
            {
                if (Changed(entry.Property("CreatedAt")) || Changed(entry.Property("CreatedBy")))
                    throw new InvalidOperationException("Creation metadata cannot be changed after insertion.");
                foreach (var property in entry.Properties)
                    if (AuditProperties.Contains(property.Metadata.Name) || !Changed(property))
                        property.IsModified = false;
                if (!entry.Properties.Any(property => property.IsModified))
                {
                    RestoreUpdate(entry);
                    continue;
                }
            }

            metadata ??= AuditWriteMetadata.Capture(actor, clock);
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = metadata.Value.UtcNow;
                entry.Property("CreatedBy").CurrentValue = metadata.Value.ActorId;
                entry.Property("UpdatedAt").CurrentValue = null;
                entry.Property("UpdatedBy").CurrentValue = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedAt").CurrentValue = metadata.Value.UtcNow;
                entry.Property("UpdatedBy").CurrentValue = metadata.Value.ActorId;
                entry.Property("UpdatedAt").IsModified = true;
                entry.Property("UpdatedBy").IsModified = true;
            }
        }
    }

    private static bool Changed(PropertyEntry property) =>
        !property.Metadata.GetValueComparer()!.Equals(property.OriginalValue, property.CurrentValue);

    private static void RestoreUpdate(EntityEntry entry)
    {
        entry.Property("UpdatedAt").CurrentValue = entry.Property("UpdatedAt").OriginalValue;
        entry.Property("UpdatedBy").CurrentValue = entry.Property("UpdatedBy").OriginalValue;
        entry.Property("UpdatedAt").IsModified = false;
        entry.Property("UpdatedBy").IsModified = false;
    }
}
