using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Features.Payments.Shared;
using YallaFinance.Application.Repositories.Payments.Contracts;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Contracts;

public sealed record PaymentTenantSettings(int MaximumLinkDays, string PublicOrigin);
