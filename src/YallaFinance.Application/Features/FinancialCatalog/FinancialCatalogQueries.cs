using MediatR;
using FluentValidation;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Application.Services.Financial.Contracts;

namespace YallaFinance.Application.Features.FinancialCatalog;

public sealed record FindFinancialCustomersByDocumentQuery(string Document)
    : IRequest<NotificationResult<FinancialCatalogPageResponse<FinancialCustomerSummary>>>;

public sealed record ListFinancialCostCentersQuery(int Skip = 0, int Take = 100)
    : IRequest<NotificationResult<FinancialCatalogPageResponse<FinancialCostCenterSummary>>>;

public sealed record ListFinancialCategoriesQuery(int Skip = 0, int Take = 100)
    : IRequest<NotificationResult<FinancialCatalogPageResponse<FinancialCategorySummary>>>;

public sealed class ListFinancialCategoriesQueryValidator : AbstractValidator<ListFinancialCategoriesQuery>
{
    public ListFinancialCategoriesQueryValidator()
    {
        RuleFor(query => query.Skip).GreaterThanOrEqualTo(0).WithMessage("O início da paginação deve ser zero ou maior.");
        RuleFor(query => query.Take).InclusiveBetween(1, 500).WithMessage("Consulte entre 1 e 500 registros por página.");
    }
}

public sealed class FindFinancialCustomersByDocumentQueryValidator : AbstractValidator<FindFinancialCustomersByDocumentQuery>
{
    public FindFinancialCustomersByDocumentQueryValidator()
    {
        RuleFor(query => query.Document).Must(document => !string.IsNullOrWhiteSpace(document)
                && document.All(character => character is >= '0' and <= '9' or '.' or '-' or '/' or ' ')
                && document.Count(character => character is >= '0' and <= '9') is 11 or 14)
            .WithMessage("Informe um CPF ou CNPJ válido para a consulta.");
    }
}

public sealed class ListFinancialCostCentersQueryValidator : AbstractValidator<ListFinancialCostCentersQuery>
{
    public ListFinancialCostCentersQueryValidator()
    {
        RuleFor(query => query.Skip).GreaterThanOrEqualTo(0).WithMessage("O início da paginação deve ser zero ou maior.");
        RuleFor(query => query.Take).InclusiveBetween(1, 500).WithMessage("Consulte entre 1 e 500 registros por página.");
    }
}

public sealed class FindFinancialCustomersByDocumentQueryHandler(
    IFinancialIntegrationService financial, ICurrentTenantContext currentTenant)
    : IRequestHandler<FindFinancialCustomersByDocumentQuery, NotificationResult<FinancialCatalogPageResponse<FinancialCustomerSummary>>>
{
    public async Task<NotificationResult<FinancialCatalogPageResponse<FinancialCustomerSummary>>> Handle(
        FindFinancialCustomersByDocumentQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(currentTenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return FinancialCatalogResults.MissingTenant<FinancialCustomerSummary>();
        var result = await financial.FindCustomersByDocumentAsync(new(tenantId, request.Document), cancellationToken);
        return FinancialCatalogResults.ToNotification(result);
    }
}

public sealed class ListFinancialCostCentersQueryHandler(
    IFinancialIntegrationService financial, ICurrentTenantContext currentTenant)
    : IRequestHandler<ListFinancialCostCentersQuery, NotificationResult<FinancialCatalogPageResponse<FinancialCostCenterSummary>>>
{
    public async Task<NotificationResult<FinancialCatalogPageResponse<FinancialCostCenterSummary>>> Handle(
        ListFinancialCostCentersQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(currentTenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return FinancialCatalogResults.MissingTenant<FinancialCostCenterSummary>();
        var result = await financial.ListCostCentersAsync(new(tenantId, request.Skip, request.Take), cancellationToken);
        return FinancialCatalogResults.ToNotification(result);
    }
}

public sealed class ListFinancialCategoriesQueryHandler(
    IFinancialIntegrationService financial, ICurrentTenantContext currentTenant)
    : IRequestHandler<ListFinancialCategoriesQuery, NotificationResult<FinancialCatalogPageResponse<FinancialCategorySummary>>>
{
    public async Task<NotificationResult<FinancialCatalogPageResponse<FinancialCategorySummary>>> Handle(
        ListFinancialCategoriesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(currentTenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return FinancialCatalogResults.MissingTenant<FinancialCategorySummary>();
        var result = await financial.ListCategoriesAsync(new(tenantId, request.Skip, request.Take), cancellationToken);
        return FinancialCatalogResults.ToNotification(result);
    }
}

internal static class FinancialCatalogResults
{
    internal static NotificationResult<FinancialCatalogPageResponse<T>> MissingTenant<T>()
        => NotificationResult<FinancialCatalogPageResponse<T>>.Failure(new NotificationError(
            ErrorCodes.Unauthorized, "Sessão sem empresa ativa válida."));

    internal static NotificationResult<FinancialCatalogPageResponse<T>> ToNotification<T>(FinancialCatalogResult<T> result)
    {
        if (result.Success && result.ConnectionId is { } connectionId && connectionId != Guid.Empty)
            return NotificationResult<FinancialCatalogPageResponse<T>>.Success(new(connectionId, result.Items, result.TotalCount));
        var validation = !result.Success && result.Error == FinancialCatalogError.Validation;
        var message = result.Error switch
        {
            FinancialCatalogError.Validation => "Confira o documento e os parâmetros da consulta.",
            FinancialCatalogError.ConfigurationUnavailable => "A conexão com o sistema financeiro está ausente, desabilitada ou com configuração inválida. Revise as configurações da conexão.",
            FinancialCatalogError.ProviderUnauthorized => "O sistema financeiro recusou o acesso da integração. Verifique a credencial e as permissões da conexão.",
            FinancialCatalogError.ProviderNotFound => "O endereço de consulta não foi encontrado no sistema financeiro. Verifique a URL configurada na conexão.",
            FinancialCatalogError.RateLimited => "O limite de consultas do sistema financeiro foi atingido. Aguarde alguns instantes e tente novamente.",
            FinancialCatalogError.ProviderRejected => "O sistema financeiro rejeitou os parâmetros da consulta. Contate o suporte para verificar a requisição da integração.",
            FinancialCatalogError.InvalidResponse => "O sistema financeiro retornou dados inválidos ou uma paginação inconsistente. Tente novamente; se persistir, contate o suporte.",
            FinancialCatalogError.OperationNotSupported => "A integração financeira configurada não oferece esta consulta.",
            _ => "Não foi possível acessar o sistema financeiro. Verifique a disponibilidade do serviço e tente novamente."
        };
        return NotificationResult<FinancialCatalogPageResponse<T>>.Failure(new NotificationError(
            validation ? ErrorCodes.Validation : ErrorCodes.ServiceUnavailable,
            message));
    }
}
