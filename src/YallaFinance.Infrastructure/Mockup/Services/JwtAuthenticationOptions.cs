using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;


using YallaFinance.Application.Common.Context;
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
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Services.Payments.Pix;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Services.Financial;

namespace YallaFinance.Infrastructure.Mockup.Services;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Authentication:Jwt";
    public string Issuer { get; set; } = "YallaFinance";
    public string Audience { get; set; } = "YallaFinance.Frontend";
    public string SigningKey { get; set; } = "yalla-finance-development-signing-key-change-me-2026";
    public int LifetimeMinutes { get; set; } = 60;
}
