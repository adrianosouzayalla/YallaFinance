using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Infrastructure.Database.Generated;

namespace YallaFinance.Infrastructure.Services.Authentication;

/// <summary>
/// Stores forced-password-change tickets in core.password_change_ticket, shared by every API instance. Only the SHA-256 hash
/// of the ticket is stored. A ticket lasts 15 minutes and is valid until it is marked as used (feature 28, phase 4).
/// </summary>
public sealed class PasswordChangeTicketStore(YallaFinanceDbContext db, TimeProvider clock) : IPasswordChangeTicketStore
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public async Task<string> IssueAsync(string identifier, string? tenantId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        // Expired tickets are removed when new ones are issued (feature 28, phase 4, decision D-3).
        await db.PasswordChangeTickets.Where(ticket => ticket.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
        var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        db.PasswordChangeTickets.Add(new YallaFinance.Infrastructure.Database.Generated.Entities.PasswordChangeTicket
        {
            Id = Guid.NewGuid(),
            TicketHash = HashOf(ticket),
            Login = identifier,
            TenantId = Guid.Parse(tenantId ?? throw new InvalidOperationException("A ticket requires the company chosen at login.")),
            CreatedAt = now,
            ExpiresAt = now + Lifetime,
        });
        await db.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<PasswordChangeTicket?> FindAsync(string ticket, CancellationToken cancellationToken)
    {
        var hash = HashOf(ticket);
        var now = clock.GetUtcNow().UtcDateTime;
        var row = await db.PasswordChangeTickets.AsNoTracking()
            .Where(stored => stored.TicketHash == hash && stored.UsedAt == null && stored.ExpiresAt > now)
            .Select(stored => new { stored.Login, stored.TenantId })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new PasswordChangeTicket(row.Login, row.TenantId.ToString());
    }

    public async Task MarkUsedAsync(string ticket, CancellationToken cancellationToken)
    {
        var hash = HashOf(ticket);
        var now = clock.GetUtcNow().UtcDateTime;
        await db.PasswordChangeTickets.Where(stored => stored.TicketHash == hash && stored.UsedAt == null)
            .ExecuteUpdateAsync(stored => stored.SetProperty(row => row.UsedAt, now), cancellationToken);
    }

    private static string HashOf(string ticket) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(ticket))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
