using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Repositories.Connections;

namespace YallaFinance.Application.Features.Connections.Providers.Nibo;

public sealed record SaveCompanyConnectionMappingCommand(Guid ConnectionId, Guid? MappingId,
    string MappingType, Guid InternalId, string ExternalValue) : IRequest<NotificationResult<Guid>>;
public sealed record RemoveCompanyConnectionMappingCommand(Guid ConnectionId, Guid MappingId) : IRequest<NotificationResult<Guid>>;

public sealed class SaveCompanyConnectionMappingCommandValidator : AbstractValidator<SaveCompanyConnectionMappingCommand>
{
    public SaveCompanyConnectionMappingCommandValidator()
    {
        RuleFor(command => command.ConnectionId).NotEmpty().WithMessage("Informe um mapeamento válido.");
        RuleFor(command => command.MappingId).Must(mappingId => mappingId is null || mappingId != Guid.Empty)
            .WithMessage("Informe um mapeamento válido.");
        RuleFor(command => command.InternalId).NotEmpty().WithMessage("Informe um mapeamento válido.");
        RuleFor(command => command.MappingType).Must(mappingType => mappingType is "Category" or "ReceiptAccount")
            .WithMessage("Informe um mapeamento válido.");
        RuleFor(command => command.ExternalValue)
            .Must(value => Guid.TryParse(value, out var externalId) && externalId != Guid.Empty)
            .WithMessage("Informe um mapeamento válido.");
    }
}

public sealed class RemoveCompanyConnectionMappingCommandValidator : AbstractValidator<RemoveCompanyConnectionMappingCommand>
{
    public RemoveCompanyConnectionMappingCommandValidator()
    {
        RuleFor(command => command.ConnectionId).NotEmpty().WithMessage("Informe um mapeamento válido.");
        RuleFor(command => command.MappingId).NotEmpty().WithMessage("Informe um mapeamento válido.");
    }
}

public sealed class SaveCompanyConnectionMappingCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository writer, TimeProvider clock)
    : IRequestHandler<SaveCompanyConnectionMappingCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveCompanyConnectionMappingCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        if (request.ConnectionId == Guid.Empty || request.MappingId == Guid.Empty || request.InternalId == Guid.Empty
            || request.MappingType is not ("Category" or "ReceiptAccount")
            || !Guid.TryParse(request.ExternalValue, out var externalId) || externalId == Guid.Empty)
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe um mapeamento válido."));
        return await writer.SaveMappingAsync(tenantId, request.ConnectionId, request.MappingId, request.MappingType,
            request.InternalId, externalId.ToString(), clock.GetUtcNow().UtcDateTime, cancellationToken);
    }
}

public sealed class RemoveCompanyConnectionMappingCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository writer)
    : IRequestHandler<RemoveCompanyConnectionMappingCommand, NotificationResult<Guid>>
{
    public Task<NotificationResult<Guid>> Handle(RemoveCompanyConnectionMappingCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return Task.FromResult(NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors));
        if (request.ConnectionId == Guid.Empty || request.MappingId == Guid.Empty)
            return Task.FromResult(NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe um mapeamento válido.")));
        return writer.RemoveMappingAsync(tenantId, request.ConnectionId, request.MappingId, cancellationToken);
    }
}
