using Npgsql;
using YallaFinance.Application.Common.Context;

namespace YallaFinance.Infrastructure.Database.Auditing;

public readonly record struct AuditWriteMetadata(Guid ActorId, DateTime UtcNow)
{
    public static AuditWriteMetadata Capture(CurrentExecutionActorContext actor, TimeProvider clock) =>
        new(actor.RequireActorId(), clock.GetUtcNow().UtcDateTime);

    internal void AddParameters(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("auditActor", ActorId);
        command.Parameters.AddWithValue("auditNow", UtcNow);
    }
}
