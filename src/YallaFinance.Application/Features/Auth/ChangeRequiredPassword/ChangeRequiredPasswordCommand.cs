using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Users;
using YallaFinance.Application.Services.Authentication;

namespace YallaFinance.Application.Features.Auth.ChangeRequiredPassword;

/// <summary>
/// Replaces a password when the login reported PASSWORD_CHANGE_REQUIRED. Needs the ticket issued by that login, the current
/// password and the new password with its confirmation. The ticket is removed only after a successful change.
/// </summary>
public sealed record ChangeRequiredPasswordCommand(string Email, string Ticket, string CurrentPassword, string NewPassword, string Confirmation)
    : IRequest<NotificationResult>;

public sealed class ChangeRequiredPasswordCommandValidator : AbstractValidator<ChangeRequiredPasswordCommand>
{
    public ChangeRequiredPasswordCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithMessage("Informe o login.");
        RuleFor(command => command.Ticket).NotEmpty().WithMessage("Entre novamente para trocar a senha.");
        RuleFor(command => command.CurrentPassword).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(command => command.NewPassword).NotEmpty().WithMessage("Informe a nova senha.");
        RuleFor(command => command.Confirmation).NotEmpty().WithMessage("Confirme a nova senha.");
    }
}

public sealed class ChangeRequiredPasswordCommandHandler(
    IUserIdentityRepository identities,
    IUserPasswordHasher passwords,
    IPasswordChangeTicketStore tickets,
    CurrentExecutionActorContext actor)
    : IRequestHandler<ChangeRequiredPasswordCommand, NotificationResult>
{
    public Task<NotificationResult> Handle(ChangeRequiredPasswordCommand request, CancellationToken cancellationToken) =>
        identities.ExecuteLockedAsync(token => ChangeAsync(request, token), cancellationToken);

    private async Task<NotificationResult> ChangeAsync(ChangeRequiredPasswordCommand request, CancellationToken token)
    {
        var ticket = await tickets.FindAsync(request.Ticket, token);
        if (ticket is null || !string.Equals(ticket.Identifier.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return NotificationResult.Failure(new NotificationError(ErrorCodes.Unauthorized, "INVALID_TICKET"));

        if (request.NewPassword != request.Confirmation)
            return NotificationResult.Failure(new NotificationError(ErrorCodes.Validation, "PASSWORD_CONFIRMATION_MISMATCH"));

        var user = await identities.ResolveAsync(ticket.Identifier, token);
        if (user is null || !passwords.Verify(user.PasswordHash, request.CurrentPassword))
            return NotificationResult.Failure(new NotificationError(ErrorCodes.Unauthorized, "INVALID_CREDENTIALS"));

        if (!user.MustChangePassword)
        {
            await tickets.MarkUsedAsync(request.Ticket, token);
            return NotificationResult.Failure(new NotificationError(ErrorCodes.Conflict, "PASSWORD_CHANGE_NOT_REQUIRED"));
        }

        // The user is validated by the ticket and the current password, so the user is the trusted actor of this write (audit rule).
        actor.SetAuthenticatedUser(user.Id);
        await identities.ReplacePasswordAsync(user.Id, request.NewPassword, token);
        // Marked as used in the same unit of work as the password change (feature 28, phase 4).
        await tickets.MarkUsedAsync(request.Ticket, token);
        return NotificationResult.Success();
    }
}
