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

public sealed record ActiveSessionItem(string DisplayName, string? Email, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, bool IsCurrent, string RowKey);
