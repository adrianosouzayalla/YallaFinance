using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts;

public sealed record ChargeItemIntegrationRequest(Guid ItemId, CreateChargeData Data, string? Provider);
