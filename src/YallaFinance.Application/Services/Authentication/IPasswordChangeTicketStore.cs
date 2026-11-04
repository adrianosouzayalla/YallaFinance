namespace YallaFinance.Application.Services.Authentication;

/// <summary>Short-lived, single-purpose ticket that lets a user with a forced password change replace the password. It is not a session.</summary>
public sealed record PasswordChangeTicket(string Identifier, string? TenantId);

/// <summary>Tickets are shared by every API instance through the database (feature 28, phase 4).</summary>
public interface IPasswordChangeTicketStore
{
    Task<string> IssueAsync(string identifier, string? tenantId, CancellationToken cancellationToken);
    Task<PasswordChangeTicket?> FindAsync(string ticket, CancellationToken cancellationToken);
    Task MarkUsedAsync(string ticket, CancellationToken cancellationToken);
}
