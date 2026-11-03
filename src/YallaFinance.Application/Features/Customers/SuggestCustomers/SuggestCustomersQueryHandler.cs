using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Customers;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Customers.SuggestCustomers;

public sealed class SuggestCustomersQueryHandler(
    ICustomerRepository repository,
    ICurrentTenantContext context)
    : IRequestHandler<SuggestCustomersQuery, NotificationResult<IReadOnlyList<CustomerResponse>>>
{
    public async Task<NotificationResult<IReadOnlyList<CustomerResponse>>> Handle(
        SuggestCustomersQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.TenantId))
            return NotificationResult<IReadOnlyList<CustomerResponse>>.Failure(new NotificationError(
                ErrorCodes.Unauthorized,
                "Sessão sem tenant ativo."));
        if (request.Source is not (null or "all" or "persisted"))
            return NotificationResult<IReadOnlyList<CustomerResponse>>.Failure(new NotificationError(
                ErrorCodes.Validation,
                "Fonte de clientes inválida."));
        var customerSearchTerm = request.Search?.Trim() ?? "";
        if (customerSearchTerm.Length < ICustomerRepository.MinimumSearchLength)
            return NotificationResult<IReadOnlyList<CustomerResponse>>.Success([]);
        return NotificationResult<IReadOnlyList<CustomerResponse>>.Success(
            await repository.SuggestAsync(
                context.TenantId,
                customerSearchTerm,
                cancellationToken,
                request.Source == "persisted"));
    }
}
