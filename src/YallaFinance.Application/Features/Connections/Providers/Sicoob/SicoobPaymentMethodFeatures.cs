using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Domain.Payments;

namespace YallaFinance.Application.Features.Connections.Providers.Sicoob;

public sealed record SaveCompanyPaymentMethodConnectionCommand(Guid ConnectionId, Guid? LinkId,
    Guid PaymentMethodId, bool Enabled, string? InstrumentType = null, string? InternalDescription = null) : IRequest<NotificationResult<Guid>>;
public sealed record RemoveCompanyPaymentMethodConnectionCommand(Guid ConnectionId, Guid LinkId) : IRequest<NotificationResult<Guid>>;

public sealed class SaveCompanyPaymentMethodConnectionCommandValidator : AbstractValidator<SaveCompanyPaymentMethodConnectionCommand>
{
    public SaveCompanyPaymentMethodConnectionCommandValidator()
    {
        RuleFor(command => command.ConnectionId).NotEmpty().WithMessage("Informe um vínculo válido.");
        RuleFor(command => command.LinkId).Must(linkId => linkId is null || linkId != Guid.Empty)
            .WithMessage("Informe um vínculo válido.");
        RuleFor(command => command.PaymentMethodId).NotEmpty().WithMessage("Informe um vínculo válido.");
    }
}

public sealed class RemoveCompanyPaymentMethodConnectionCommandValidator : AbstractValidator<RemoveCompanyPaymentMethodConnectionCommand>
{
    public RemoveCompanyPaymentMethodConnectionCommandValidator()
    {
        RuleFor(command => command.ConnectionId).NotEmpty().WithMessage("Informe um vínculo válido.");
        RuleFor(command => command.LinkId).NotEmpty().WithMessage("Informe um vínculo válido.");
    }
}

public sealed class SaveCompanyPaymentMethodConnectionCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository writer, TimeProvider clock)
    : IRequestHandler<SaveCompanyPaymentMethodConnectionCommand, NotificationResult<Guid>>
{
    public Task<NotificationResult<Guid>> Handle(SaveCompanyPaymentMethodConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return Task.FromResult(NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors));
        if (request.ConnectionId == Guid.Empty || request.LinkId == Guid.Empty || request.PaymentMethodId == Guid.Empty)
            return Task.FromResult(NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe um vínculo válido.")));
        PaymentInstrumentType? instrument = null;
        if (request.InstrumentType is not null)
        {
            if (!Enum.TryParse<PaymentInstrumentType>(request.InstrumentType, out var parsed) || !Enum.IsDefined(parsed)
                || parsed.ToString() != request.InstrumentType)
                return Task.FromResult(NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe uma modalidade válida.")));
            instrument = parsed;
        }
        var configuration = request.InstrumentType is not null || request.InternalDescription is not null
            ? new CompanyPaymentMethodConfiguration(instrument, request.InternalDescription) : null;
        return writer.SavePaymentMethodAsync(tenantId, request.ConnectionId, request.LinkId,
            request.PaymentMethodId, request.Enabled, clock.GetUtcNow().UtcDateTime, cancellationToken, configuration);
    }
}

public sealed class RemoveCompanyPaymentMethodConnectionCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository writer)
    : IRequestHandler<RemoveCompanyPaymentMethodConnectionCommand, NotificationResult<Guid>>
{
    public Task<NotificationResult<Guid>> Handle(RemoveCompanyPaymentMethodConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return Task.FromResult(NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors));
        if (request.ConnectionId == Guid.Empty || request.LinkId == Guid.Empty)
            return Task.FromResult(NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.Validation, "Informe um vínculo válido.")));
        return writer.RemovePaymentMethodAsync(tenantId, request.ConnectionId, request.LinkId, cancellationToken);
    }
}
