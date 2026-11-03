using Microsoft.Extensions.DependencyInjection;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.SettleCharge;

public static class ChargeSettlementRegistration
{
    public static IServiceCollection AddChargeSettlementJobs(this IServiceCollection services)
        => services.AddJob<SettleChargePayload, SettleChargeHandler>(
            SettleChargeJob.JobType,
            SettleChargeJob.JobTitle);
}
