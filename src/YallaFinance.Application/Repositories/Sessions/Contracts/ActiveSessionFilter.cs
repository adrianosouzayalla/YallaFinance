using YallaFinance.Application.Entities.Tenants;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application.Repositories.Charges.Contracts;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Debits.Contracts;
using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application.Repositories.PaymentMethods.Contracts;
using YallaFinance.Application.Services.Authentication.Contracts;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Repositories.Sessions.Contracts;

public sealed record ActiveSessionFilter(int Page = 1, int PageSize = 20,
    string SortBy = "createdAt", string SortDirection = "desc")
{
    public void Validate()
    {
        if (Page < 1 || Page > int.MaxValue / 100 || PageSize is < 1 or > 100
            || SortBy is not ("name" or "email" or "createdAt" or "expiresAt")
            || SortDirection is not ("asc" or "desc"))
            throw new ArgumentException("Paginação ou ordenação de sessões inválida.");
    }
}
