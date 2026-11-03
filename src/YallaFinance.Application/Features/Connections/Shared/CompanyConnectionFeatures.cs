using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Company;
using YallaFinance.Application.Features.Connections.Providers.Nibo;
using YallaFinance.Application.Features.Connections.Providers.Sicoob;
using YallaFinance.Application.Features.Connections.Providers.Asaas;
using YallaFinance.Application.Repositories.Connections;

namespace YallaFinance.Application.Features.Connections.Shared;

public sealed record ListCompanyConnectionsQuery(CompanyConnectionFilter Filter) : IRequest<NotificationResult<CompanyPage<CompanyConnectionSummary>>>;
public sealed record GetCompanyConnectionQuery(Guid ConnectionId, CompanyConnectionDetailFilter Filter) : IRequest<NotificationResult<CompanyConnectionDetail>>;
public sealed record SaveCompanyConnectionCommand(Guid? ConnectionId, string Name, string Provider, string Group,
    bool Enabled) : IRequest<NotificationResult<Guid>>;
public sealed record CompanyConnectionSettingInput(string Group, string Key, string? Value);
public sealed record SaveCompanyConnectionSettingsCommand(Guid ConnectionId,
    IReadOnlyList<CompanyConnectionSettingInput> Settings) : IRequest<NotificationResult<Guid>>;
public sealed class ListCompanyConnectionsQueryValidator : AbstractValidator<ListCompanyConnectionsQuery>
{
    public ListCompanyConnectionsQueryValidator()
    {
        RuleFor(x => x.Filter).NotNull().WithMessage("Informe os filtros de conexão.")
            .SetValidator(new CompanyConnectionFilterValidator());
    }
}

public sealed class GetCompanyConnectionQueryValidator : AbstractValidator<GetCompanyConnectionQuery>
{
    public GetCompanyConnectionQueryValidator()
    {
        RuleFor(x => x.ConnectionId).NotEmpty().WithMessage("Informe uma conexão válida.");
        RuleFor(x => x.Filter).NotNull().WithMessage("Informe os filtros da conexão.")
            .SetValidator(new CompanyConnectionDetailFilterValidator());
    }
}

public sealed class CompanyConnectionFilterValidator : AbstractValidator<CompanyConnectionFilter>
{
    public CompanyConnectionFilterValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Provider).Must(x => x is null or "Nibo" or "Sicoob" or "Asaas");
        RuleFor(x => x.Group).Must(x => x is null or "FinancialSystem" or "PaymentProvider");
        RuleFor(x => x.SortBy).Must(x => x is "name" or "provider" or "group" or "enabled" or "createdAt" or "updatedAt");
        RuleFor(x => x.SortDirection).Must(x => x is "asc" or "desc");
    }
}

public sealed class CompanyConnectionDetailFilterValidator : AbstractValidator<CompanyConnectionDetailFilter>
{
    public CompanyConnectionDetailFilterValidator()
    {
        RuleFor(x => x.CategoryPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ReceiptAccountPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PaymentMethodPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.CategorySortBy).Must(IsMappingSort);
        RuleFor(x => x.ReceiptAccountSortBy).Must(IsMappingSort);
        RuleFor(x => x.PaymentMethodSortBy).Must(x => x is "paymentMethodName" or "enabled" or "createdAt" or "updatedAt");
        RuleFor(x => x.CategorySortDirection).Must(IsDirection);
        RuleFor(x => x.ReceiptAccountSortDirection).Must(IsDirection);
        RuleFor(x => x.PaymentMethodSortDirection).Must(IsDirection);
    }
    private static bool IsMappingSort(string value) => value is "internalName" or "internalValue" or "externalValue" or "createdAt" or "updatedAt";
    private static bool IsDirection(string value) => value is "asc" or "desc";
}

public sealed class SaveCompanyConnectionCommandValidator : AbstractValidator<SaveCompanyConnectionCommand>
{
    public SaveCompanyConnectionCommandValidator()
    {
        RuleFor(command => command.ConnectionId)
            .Must(connectionId => connectionId is null || connectionId != Guid.Empty)
            .WithMessage("Informe uma conexão válida.");
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Informe o nome da conexão.");
        RuleFor(command => command.Provider)
            .Must((command, provider) => provider switch
            {
                "Nibo" => command.Group == "FinancialSystem",
                "Sicoob" or "Asaas" => command.Group == "PaymentProvider",
                _ => false
            })
            .WithMessage("Provider ou grupo de conexão inválido.");
    }
}

public sealed class SaveCompanyConnectionSettingsCommandValidator : AbstractValidator<SaveCompanyConnectionSettingsCommand>
{
    public SaveCompanyConnectionSettingsCommandValidator()
    {
        RuleFor(command => command.ConnectionId)
            .NotEmpty()
            .WithMessage("Informe a conexão e suas configurações.");
        RuleFor(command => command.Settings)
            .NotNull()
            .WithMessage("Informe a conexão e suas configurações.");
        RuleFor(command => command.Settings)
            .Must(settings => settings is null || settings.All(setting =>
                setting is not null && !string.IsNullOrWhiteSpace(setting.Group) && !string.IsNullOrWhiteSpace(setting.Key)))
            .WithMessage("Configurações do provider inválidas.");
    }
}

public sealed class ListCompanyConnectionsQueryHandler(ICurrentTenantContext context, ICompanyConnectionRepository repository)
    : IRequestHandler<ListCompanyConnectionsQuery, NotificationResult<CompanyPage<CompanyConnectionSummary>>>
{
    public async Task<NotificationResult<CompanyPage<CompanyConnectionSummary>>> Handle(ListCompanyConnectionsQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyPage<CompanyConnectionSummary>>();
        var validation = new ListCompanyConnectionsQueryValidator().Validate(request);
        if (!validation.IsValid) return NotificationResult<CompanyPage<CompanyConnectionSummary>>.Failure(validation.Errors.Select(x => new NotificationError(ErrorCodes.Validation, "Filtro de conexão inválido.", x.PropertyName)));
        return NotificationResult<CompanyPage<CompanyConnectionSummary>>.Success(await repository.ListAsync(tenantId, request.Filter, cancellationToken));
    }
}

public sealed class GetCompanyConnectionQueryHandler(ICurrentTenantContext context, ICompanyConnectionRepository repository)
    : IRequestHandler<GetCompanyConnectionQuery, NotificationResult<CompanyConnectionDetail>>
{
    public async Task<NotificationResult<CompanyConnectionDetail>> Handle(GetCompanyConnectionQuery request, CancellationToken cancellationToken)
    {
        if (!CompanyTenant.TryGet(context, out var tenantId)) return CompanyTenant.Unauthorized<CompanyConnectionDetail>();
        var validation = new GetCompanyConnectionQueryValidator().Validate(request);
        if (!validation.IsValid) return NotificationResult<CompanyConnectionDetail>.Failure(new NotificationError(ErrorCodes.Validation, "Parâmetros da conexão inválidos."));
        var detail = await repository.GetAsync(tenantId, request.ConnectionId, request.Filter, cancellationToken);
        return detail is null ? NotificationResult<CompanyConnectionDetail>.Failure(new NotificationError(ErrorCodes.NotFound, "Conexão não encontrada.")) : NotificationResult<CompanyConnectionDetail>.Success(detail);
    }
}

public sealed class SaveCompanyConnectionCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ICompanyConnectionRepository writer, TimeProvider clock)
    : IRequestHandler<SaveCompanyConnectionCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveCompanyConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        var validation = ValidateConnection(request);
        if (validation is not null) return NotificationResult<Guid>.Failure(validation);
        return await writer.SaveConnectionAsync(tenantId, request.ConnectionId, request.Name, request.Provider,
            request.Group, request.Enabled, clock.GetUtcNow().UtcDateTime, cancellationToken);
    }

    private static NotificationError? ValidateConnection(SaveCompanyConnectionCommand request)
    {
        if (request.ConnectionId == Guid.Empty)
            return new(ErrorCodes.Validation, "Informe uma conexão válida.", nameof(request.ConnectionId));
        if (string.IsNullOrWhiteSpace(request.Name))
            return new(ErrorCodes.Validation, "Informe o nome da conexão.", nameof(request.Name));
        var expectedGroup = request.Provider switch
        {
            "Nibo" => "FinancialSystem",
            "Sicoob" or "Asaas" => "PaymentProvider",
            _ => null
        };
        return expectedGroup is null || request.Group != expectedGroup
            ? new(ErrorCodes.Validation, "Provider ou grupo de conexão inválido.", nameof(request.Provider))
            : null;
    }
}

public sealed class SaveCompanyConnectionSettingsCommandHandler(ICurrentTenantContext tenantContext,
    CurrentSessionContext sessionContext, ISender sender, ICompanyConnectionRepository repository)
    : IRequestHandler<SaveCompanyConnectionSettingsCommand, NotificationResult<Guid>>
{
    public async Task<NotificationResult<Guid>> Handle(SaveCompanyConnectionSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!CompanySettingsWrite.TryGetActor(tenantContext, sessionContext, out var tenantId, out _))
            return NotificationResult<Guid>.Failure(CompanySettingsWrite.Unauthorized().Errors);
        if (request.ConnectionId == Guid.Empty || request.Settings is null)
            return Invalid("Informe a conexão e suas configurações.");
        if (!ConnectionSettingsValidator.TryFlattenSettings(request.Settings, out _, out var error))
            return NotificationResult<Guid>.Failure(error!);
        var connection = await repository.GetRuntimeAsync(tenantId, request.ConnectionId, cancellationToken);
        if (connection is null || connection.TenantId != tenantId || connection.Id != request.ConnectionId)
            return NotificationResult<Guid>.Failure(new NotificationError(ErrorCodes.NotFound, "Conexão não encontrada."));
        return connection.Provider switch
        {
            "Nibo" when connection.Group == "FinancialSystem" => await sender.Send(new SaveNiboConnectionSettingsCommand(request.ConnectionId, request.Settings), cancellationToken),
            "Sicoob" when connection.Group == "PaymentProvider" => await sender.Send(new SaveSicoobConnectionSettingsCommand(request.ConnectionId, request.Settings), cancellationToken),
            "Asaas" when connection.Group == "PaymentProvider" => await sender.Send(new SaveAsaasConnectionSettingsCommand(request.ConnectionId, request.Settings), cancellationToken),
            _ => Invalid("Configurações do provider inválidas.")
        };
    }

    private static NotificationResult<Guid> Invalid(string message) => NotificationResult<Guid>.Failure(
        new NotificationError(ErrorCodes.Validation, message));
}

